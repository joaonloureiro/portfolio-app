using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoaoLoureiro.Portfolio.Domain.Entities
{
    public class ContactMessage(string name, Email replyToEmail, string message)
    {
        public string Name { get; private set; } = name;
        public Email ReplyToEmail { get; private set; } = replyToEmail;
        public string Message { get; private set; } = message;
    }
}
