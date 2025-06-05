using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Acme.Dtos
{

    public class AuthorizationContextDto
    {
        public Identifier identifier { get; set; }
        public string status { get; set; }
        public DateTime expires { get; set; }
        public Challenge[] challenges { get; set; }
    }

    public class Identifier
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    public class Challenge
    {
        public string type { get; set; }
        public string url { get; set; }
        public string status { get; set; }
        public string token { get; set; }
    }

}
