using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices; 
using System.Threading.Tasks;

namespace Sempiler.Diagnostics
{
    public enum MessageKind
    {
        Info,
        Warning,
        Error
    }

    public class Result<T>
    {
        public T Value;
        public MessageCollection Messages;

        public void Deconstruct(out MessageCollection messages, out T value)
        {
            value = Value;
            messages = Messages;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public U AddMessages<U>(Result<U> result)
        {
            AddMessages(result.Messages);

            return result.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMessages(MessageCollection m)
        {
            if (m != null)
            {
                (Messages ?? (Messages = new MessageCollection())).AddAll(m);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMessages(params Message[] m)
        {
            if (m != null && m.Length > 0)
            {
                if(Messages == null)
                {
                    Messages = new MessageCollection();
                }

                for(int i = 0; i < m.Length; ++i)
                {
                    Messages.Add(m[i]);
                }
            }
        }

        // public void AddMessage(Message m)
        // {
        //     if (m != null)
        //     {
        //         (Messages ?? (Messages = new Messages())).Add(m);
        //     }
        // }

        // public void AddError(Error error)
        // {
        //     if(Messages == null)
        //     {
        //         Messages = new Messages();
        //     }

        //     (Messages.Errors ?? (Messages.Errors = new List<Error>())).Add(error);
        // }

        // public void AddWarning(Warning warning)
        // {
        //     if(Messages == null)
        //     {
        //         Messages = new Messages();
        //     }

        //     (Messages.Warnings ?? (Messages.Warnings = new List<Warning>())).Add(warning);
        // }

        // public void AddInfo(Info info)
        // {
        //     if(Messages == null)
        //     {
        //         Messages = new Messages();
        //     }

        //     (Messages.Infos ?? (Messages.Infos = new List<Info>())).Add(info);
        // }
    }



    public class MessageCollection
    {
        public List<Message> Infos;
        public List<Message> Errors;
        public List<Message> Warnings;

        public void Deconstruct(out List<Message> infos, out List<Message> warnings, out List<Message> errors)
        {
            infos = Infos;
            errors = Errors;
            warnings = Warnings;
        }

        public void Add(Message m)
        {
            switch (m.Kind)
            {
                case MessageKind.Info:
                    (Infos ?? (Infos = new List<Message>())).Add(m);
                    break;

                case MessageKind.Warning:
                    (Warnings ?? (Warnings = new List<Message>())).Add(m);
                    break;

                case MessageKind.Error:
                    (Errors ?? (Errors = new List<Message>())).Add(m);
                    break;
            }
        }

        public void AddAll(MessageCollection m)
        {
            if (m == null)
            {
                return;
            }

            if (m.Errors != null)
            {
                (Errors ?? (Errors = new List<Message>())).AddRange(m.Errors);
            }

            if (m.Warnings != null)
            {
                (Warnings ?? (Warnings = new List<Message>())).AddRange(m.Warnings);
            }

            if (m.Infos != null)
            {
                (Infos ?? (Infos = new List<Message>())).AddRange(m.Infos);
            }
        }
    }

    // public interface Message
    // {
    //     MessageKind Kind { get; set; }
    //     string Description { get; set; }
    //     object Data { get; set; }
    // }

    public class FileMarker
    {
        public IFileLocation File;

        public Range LineNumber;

        public Range ColumnIndex;

        public Range Pos;
    }

    public /*abstract*/ class Message
    {
        // public PhaseKind Phase;

        public MessageKind Kind;

        public IEnumerable<string> Tags;

        public string Description;

        // public Dictionary<string, object> Data;

        public FileMarker Hint;

        public Message(/*PhaseKind phase, */MessageKind kind, string description)
        {
            // Phase = phase;
            Kind = kind;
            Description = description;
        }
    }

    public static class MessageHelpers
    {
        public static Message Clone(Message m)
        {
            return new Message(m.Kind, m.Description)
            {
                Hint = m.Hint,
                Tags = m.Tags
            };
        }   
    }

    public class ExceptionMessage : Message
    {
        public readonly Exception Exception;

        public ExceptionMessage(/*PhaseKind phase, */Exception exception) : this(/*phase, */exception.Message, exception)
        {
        }

        public ExceptionMessage(/*PhaseKind phase, */string description, Exception exception) : base(/*phase, */MessageKind.Error, description)
        {
            Exception = exception;
        }
    }

    // public interface Info : Message
    // {
    // }

    // public interface Error : Message
    // {
    //     // int Code { get; set; }
    // }

    // public interface Warning : Message
    // {
    // }

    // public class Error : Message//, Error
    // {
    //     // public int Code { get; set; }

    //     public Error(string description) : base(MessageKind.Error, description)
    //     {
    //         // Code = (int)ErrorCode.General;
    //     }
    // }

    // public class Warning : Message//, Warning
    // {
    //     public Warning(string description) : base(MessageKind.Warning, description)
    //     {
    //     }
    // }

    // public class Info : Message//, Info
    // {
    //     public Info(string description) : base(MessageKind.Info, description)
    //     {
    //     }
    // }

    // public enum ErrorCode
    // {
    //     General,
    //     IllegalArgument,
    // }

    public static class DiagnosticsHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasErrors<T>(Result<T> result)
        {
            // [dho] TODO if we ever add a flag for cancellation
            // then we can consider that a terminal condition and check
            // that here too - 23/08/18
            return result.Messages?.Errors?.Count > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasErrors(MessageCollection m)
        {
            return m?.Errors?.Count > 0;
        }

        // public static async Task<Result<T>> SafeAwait<T>(Task<Result<T>> task)
        // {
        //     try
        //     {
        //         return await task;
        //     }
        //     catch(Exception e)
        //     {
        //         var result = new Result<T>();

        //         result.AddError(
        //             Sempiler.Diagnostics.Helpers.CreateErrorFromException(e)
        //         );

        //         return result;
        //     }
        // }

        // public static Warning CreateWarning(string description, object data = null)
        // {
        //     return new Warning(description)
        //     {
        //         Data = data
        //     };
        // }


        // public static Error CreateError(string description, object data = null)
        // {
        //     return new Error(description)
        //     {
        //         Data = data
        //     };
        // }

        public static ExceptionMessage CreateErrorFromException(System.Exception e/*, PhaseKind phase*/)
        {
            return new ExceptionMessage(/*phase, */e);
        }

        public static ExceptionMessage CreateErrorFromException(System.Exception e, string description/*, PhaseKind phase*/)
        {
            return new ExceptionMessage(/*phase, */description, e);
        }
    }
}

