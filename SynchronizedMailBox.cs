using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadSynchronization
{
    //This class extends the MailBox, by overriding the read and write methods.
    //The override must call the original implementation, which contain races, protecting the critical sections from races.
    //You cannot define a new message array or any other data structue for messages here.
    //You can, however, add any other fields as you see fit.
    class SynchronizedMailBox : MailBox
    {
        private readonly Mutex mutex = new Mutex();
        private readonly Semaphore writeSemaphore;
        private readonly Semaphore readSemaphore;

        // מאתחל תיבת דואר מסונכרנת עם מספר הודעות מקסימלי נתון
        public SynchronizedMailBox(int cMaxMessages) : base(cMaxMessages)
        {
            writeSemaphore = new Semaphore(cMaxMessages, cMaxMessages); // אתחול ליכולת מקסימלית
            readSemaphore = new Semaphore(0, cMaxMessages); // אתחול ללא הודעות לקריאה
        }



        // מאתחל תיבת דואר מסונכרנת עם מספר הודעות מקסימלי ברירת מחדל של 1000
        public SynchronizedMailBox() : this(1000) { }


        // כותב הודעה לתיבת הדואר וממתין במידת הצורך עד לפינוי מקום
        public override void Write(Message msg)
        {
            writeSemaphore.WaitOne();  // המתנה למקום פנוי לכתיבה
            mutex.WaitOne(); // נעילה להגנה על אזור קריטי
            try
            {
                base.Write(msg);  // כתיבת ההודעה לתיבת הדואר
                readSemaphore.Release(); // שחרור לסימון שהודעה זמינה לקריאה
            }
            finally
            {
                mutex.ReleaseMutex();  // שחרור הנעילה
            }
        }


        // קורא הודעה מתיבת הדואר וממתין במידת הצורך עד שהודעה זמינה
        public override Message Read()
        {
            readSemaphore.WaitOne(); // המתנה להודעה לקריאה
            mutex.WaitOne(); // נעילה להגנה על אזור קריטי
            try
            {
                var msg = base.Read(); // קריאת ההודעה מתיבת הדואר
                writeSemaphore.Release(); // שחרור לסימון שיש מקום פנוי לכתיבה
                return msg;
            }
            finally
            {
                mutex.ReleaseMutex(); // שחרור הנעילה
            }
        }
    }
}
