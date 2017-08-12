namespace App.Utils
{
    class Message
    {
        public const int TypeMessage = 0;
        public const int TypeError = 1;

        public readonly int Type;
        public readonly int Id;
        public readonly object[] Arguments;

        public Message(int type, int id, object [] argumens)
        {
            Type = type;
            Id = id;
            Arguments = argumens;
        }

        public Message(int id, object [] argumens)
        {
            Id = id;
            Arguments = argumens;
        }

        public Message(int type, string text) : this(type, 0, new object [] { text })
        {

        }
    }
}