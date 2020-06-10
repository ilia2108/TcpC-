
using System;

namespace Server
{
    [Serializable]
    public class LoginFailedException : System.Exception
    {
        public LoginFailedException():base("Failed to login.") { }
    }

    [Serializable]
    public class SyntaxErrorException : System.Exception
    {
        public SyntaxErrorException(): base("Invalid syntax.") { }
    }


    [Serializable]
    public class TimeoutExcpetion : System.Exception
    {
        public TimeoutExcpetion(): base("Server timeout.") { }
    }

    [Serializable]
    public class WrongLogicException : System.Exception
    {
        public WrongLogicException(): base("Unexpected message.") { }
    }

}