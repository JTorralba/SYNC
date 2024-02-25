using System;

namespace Standard
{
    public class DEBUG
    {
        public void Message(string _Class, string _Message)
        {
            Console.WriteLine("{0}: {1}", _Class, _Message);
        }
    }
}
