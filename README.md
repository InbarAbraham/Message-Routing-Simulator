# Message Routing Simulation

This project simulates message routing between nodes using synchronized mailboxes in a distributed-like system.  
It was developed as part of an academic Operating Systems course.

## 🧩 Project Structure

The simulation is composed of the following main components:

- **Program.cs** – Initializes the system and starts the simulation.
- **Node.cs** – Represents a node in the network, capable of receiving and routing messages.
- **Message.cs** – Base class for all types of messages passed between nodes.
- **PacketMessage.cs** – Represents a standard data packet message.
- **RoutingMessage.cs** – Contains information used for message routing.
- **KillMessage.cs** – Special message type used to stop node activity.
- **MailBox.cs / SynchronizedMailBox.cs** – Thread-safe message queue system for communication between nodes.
- **MailBoxWriter.cs** – Utility class to send messages to specific mailboxes.

## 🔄 How It Works

1. Each node runs in its own thread and waits for messages in its mailbox.
2. Messages (such as `PacketMessage`, `RoutingMessage`, or `KillMessage`) are passed between nodes.
3. The nodes process incoming messages and may forward them to the next destination.
4. `KillMessage` is used to gracefully shut down a node after simulation ends.

