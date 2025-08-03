using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace ThreadSynchronization
{
    // This class represents a machine in the machine network graph.
    class Node
    {
        private MailBox m_mbMailBox; // Incoming mailbox of the node
        private Dictionary<int, MailBoxWriter> m_dNeighbors; // Maps node IDs to outgoing mailboxes
        private bool m_bDone; // Notifies the thread to terminate

        private static readonly Random _random = new Random(); //משמש עבור "הרעש" 
        private bool sentToWrongNode = false; // בודק אם הודעה נשלחה לצומת שגוי

        // טבלת ניתוב: מזהה צומת ->מרחק,נתב
        private Dictionary<int, (int Distance, int Router)> routingTable = new Dictionary<int, (int Distance, int Router)>();

        // אחסון הודעות שהתקבלו
        private Dictionary<int, char[]> receivedMessages = new Dictionary<int, char[]>();



        public int ID { get; private set; } //the identifier of the node

        public Node(int iID)
        {
            ID = iID;
            m_mbMailBox = new SynchronizedMailBox();
            m_dNeighbors = new Dictionary<int, MailBoxWriter>();
            m_bDone = false;

            routingTable[ID] = (0, ID); // אתחול טבלת הניתוב: המרחק לצומת עצמו הוא 0
        }

        // Returns access to the node's mailbox
        public MailBoxWriter GetMailBox()
        {
            return new MailBoxWriter(m_mbMailBox);
        }


        //  מוסיף או מעדכן ערך בטבלת הניתוב
        private bool UpdateValueRoutingTable(int node, int distance, int router)
        {
            if (!routingTable.ContainsKey(node) || distance < routingTable[node].Distance)
            {
                routingTable[node] = (distance, router); // מעדכן את טבלת הניתוב אם המרחק החדש קצר יותר
                return true; // עדכנו את הטבלה
            }
            return false; // לא עדכנו את הטבלה
        }

        // Sends routing messages to all immediate neighbors
        private void SendRoutingMessages()
        {
            var nodeDistances = routingTable.ToDictionary(keyNode => keyNode.Key, keyNode => keyNode.Value.Distance);

            foreach (var neighbor in m_dNeighbors)
            {
                var rmsg = new RoutingMessage(ID, neighbor.Key, nodeDistances);
                neighbor.Value.Send(rmsg); // שולח הודעות ניתוב לכל השכנים המידיים
            }
        }

        // Handles an incoming routing message
        private void HandleRoutingMessage(RoutingMessage rmsg)
        {
            bool ifChanged = false;

            foreach (var node in rmsg.GetAllNodes())
            {
                int newDistance = rmsg.GetDistance(node) + 1;
                if (UpdateValueRoutingTable(node, newDistance, rmsg.Sender))
                {
                    ifChanged = true;
                }
            }

            if (ifChanged)  // אם טבלת הניתוב שונתה, שולח אותה לשכנים
            {
                SendRoutingMessages();
            }
        }

        // Handles an incoming packet message
        private void HandlePacketMessage(PacketMessage pmsg)
        {
            /* הוספת רעש
            if (!sentToWrongNode && _random.NextDouble() <= 0.8) // סיכוי של 80% לשלוח את החבילה לשכן הלא נכון
            {
                sentToWrongNode = true;

                var availableRouters = m_dNeighbors.Keys.Where(key => key != pmsg.Target).ToList();
                if (availableRouters.Count > 0)
                {
                    int randomHop = availableRouters[_random.Next(availableRouters.Count)];
                    m_dNeighbors[randomHop].Send(pmsg); // שליחת החבילה לשכן הלא נכון
                    return; // סיום הטיפול בחבילה
                }
            }
             סוף חלק הרעש */ 


            // If the node is the correct target, handle the message
            if (pmsg.Target == ID)
            {
                AddOrUpdatePacket(pmsg.MessageID, pmsg.Location, pmsg.Packet, pmsg.Size);

                // Check if the message is fully received
                if (!receivedMessages[pmsg.MessageID].Contains('\0'))
                {
                    // Message fully received
                    Debug.WriteLine($"Message {pmsg.MessageID} has been fully received: " +
                                      new string(receivedMessages[pmsg.MessageID]));
                }
                return;
            }

            // Otherwise, forward the message to the next hop
            int nextHop = GetRouter(pmsg.Target);
            if (nextHop != -1)
            {
                m_dNeighbors[nextHop].Send(pmsg);
            }
        }


        // מוסיף או מעדכן חבילה שהתקבלה
        private void AddOrUpdatePacket(int messageId, int location, char packet, int size)
        {
            if (!receivedMessages.ContainsKey(messageId))
            {
                receivedMessages[messageId] = new char[size];
                for (int i = 0; i < size; i++)
                {
                    receivedMessages[messageId][i] = '\0'; // מאתחל תווים חסרים ל-\0
                }
            }

            receivedMessages[messageId][location] = packet; // מעדכן את התו במיקום הנכון
        }


        // Gets the router for a specific target
        private int GetRouter(int target)
        {
            return routingTable.ContainsKey(target) ? routingTable[target].Router : -1;  // מחזיר את הנתב לצומת מסוים
        }


        // Node (thread) main method - repeatedly checks for incoming mail and handles it.
        // When the thread is terminated using the KillMessage, outputs the routing table and the list of accepted messages
        public void Run()
        {
            SendRoutingMessages();
            while (!m_bDone)
            {
                Message msg = m_mbMailBox.Read();
                if (msg is RoutingMessage)
                {
                    HandleRoutingMessage((RoutingMessage)msg);
                }
                if (msg is PacketMessage)
                {
                    HandlePacketMessage((PacketMessage)msg);
                }
                if (msg is KillMessage)
                    m_bDone = true;
            }
            PrintRoutingTable();
            PrintAllMessages();
        }

        // Creates a thread that executes the Run method, starts it, and returns the created Thread object
        public Thread Start()
        {
            var thread = new Thread(Run);
            thread.Start();
            return thread;
        }


        // Prints the routing table
        public void PrintRoutingTable()
        {
            string s = "Routing table for " + ID + "\n";
            foreach (var node in routingTable)
            {
                s += $"{node.Key}, distance = {node.Value.Distance}, router = {node.Value.Router}\n";
            }
            Debug.WriteLine(s);
        }

        // Prints the list of accepted messages
        // If a char is missing, writes '?' instead
        public void PrintAllMessages()
        {
            Debug.WriteLine("Message list of " + ID);
            foreach (var message in receivedMessages.Values)
            {
                string s = new string(message.Select(c => c == '\0' ? '?' : c).ToArray());
                Debug.WriteLine(s);
            }
        }

        // Sets a link (immediate access) between two nodes
        public static void SetLink(Node n1, Node n2)
        {
            n1.m_dNeighbors[n2.ID] = n2.GetMailBox();
            n2.m_dNeighbors[n1.ID] = n1.GetMailBox();
        }


        //Allows the administrator to send a string message from one machine to another
        //the message must be broken into packets
        //if the node does not recognize the target (the target is not in the routing table)
        //the method returns false
        public bool SendMessage(string sMessage, int iMessageID, int iTarget)
        {
            if (!routingTable.ContainsKey(iTarget))
            {
                return false;
            }

            try
            {
                for (int i = 0; i < sMessage.Length; i++)
                {
                    var pmsg = new PacketMessage(ID, iTarget, iMessageID, sMessage[i], i, sMessage.Length);
                    int nextHop = GetRouter(iTarget);
                    if (nextHop == ID)
                    {
                        HandlePacketMessage(pmsg);
                    }
                    else
                    {
                        m_dNeighbors[nextHop].Send(pmsg);
                    }
                    // הוספת הדפסה
                    Debug.WriteLine($"Node {ID} sent packet '{sMessage[i]}' (part of message {iMessageID}) to node {nextHop}");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Node {ID}, Target {iTarget}: Error sending message: {e.Message}");
                throw;
            }

            return true;
        }
    }
}
