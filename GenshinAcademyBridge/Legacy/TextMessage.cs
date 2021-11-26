using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinAcademyBridge
{
    public class TextMessage
    {
        public readonly string Message;
        public readonly string Sender;

        public TextMessage(string sender, string message)
        {
            Sender = sender;
            Message = message;
        } 
    }
}
