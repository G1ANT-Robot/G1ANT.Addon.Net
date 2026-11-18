using G1ANT.Addon.Net.Models;
using G1ANT.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G1ANT.Addon.Net.Structures
{
    [Structure(Name = "exchangeoauth", Priority = 999, Default = 0, AutoCreate = false, Tooltip = "The structure stores OAuth details how to connect to the server")]
    public class ExchangeOAuthStructure : StructureTyped<ExchangeOAuthModel>
    {
        public static class IndexNames
        {
            public const string Host = "host";
            public const string Username = "username";            
            public const string Password = "password";
            public const string Domain = "domain";
            public const string Logger = "logger";
        }

        public ExchangeOAuthStructure(object value, string format = null, AbstractScripter scripter = null)
            : base(value, format, scripter)
        {
            Init();
        }

        public ExchangeOAuthStructure(ExchangeOAuthModel value, string format = "")
            : base(value, format)
        {
            Init();
        }

        private void Init()
        {
            Indexes.Add(IndexNames.Username);
            Indexes.Add(IndexNames.Password);
            Indexes.Add(IndexNames.Domain);
            Indexes.Add(IndexNames.Host);
            Indexes.Add(IndexNames.Logger);
        }

        public override Structure Get(string index = "")
        {
            if (string.IsNullOrWhiteSpace(index))
                throw new ArgumentException($"Index cannot be empty, possible values: {string.Join(", ", index)}", nameof(index));

            switch (index.ToLower())
            {
                case IndexNames.Username:
                    return new TextStructure(Value?.Username);
                case IndexNames.Password:
                    return new TextStructure(Value?.Password);
                case IndexNames.Host:
                    return new TextStructure(Value?.Host);
                case IndexNames.Domain:
                    return new TextStructure(Value?.Domain);
                case IndexNames.Logger:
                    return new BooleanStructure(Value?.Logger);
            }
            throw new ArgumentException($"Unknown index '{index}', possible values: {string.Join(", ", index)}", nameof(index));
        }

        public override void Set(Structure structure, string index = null)
        {
            if (string.IsNullOrWhiteSpace(index))
                throw new ArgumentException($"Index cannot be empty, possible values: {string.Join(", ", index)}", nameof(index));

            switch (index.ToLower())
            {
                case IndexNames.Username:
                    Value.Username = structure?.ToString();
                    break;
                case IndexNames.Password:
                    Value.Password = structure?.ToString();
                    break;
                case IndexNames.Domain:
                    Value.Domain = structure?.ToString();
                    break;
                case IndexNames.Host:
                    Value.Host = structure?.ToString();
                    break;
                case IndexNames.Logger:
                    if (structure == null)
                        Value.Logger = false;
                    else
                        Value.Logger = ((BooleanStructure)structure).Value;
                    break;
                default:
                    throw new ArgumentException($"Unknown index '{index}', possible values: {string.Join(", ", index)}", nameof(index));
            }
        }

        public override string ToString(string format = "")
        {
            return Value.Username;
        }

        protected override ExchangeOAuthModel Parse(object value, string format = null)
        {
            return new ExchangeOAuthModel();
        }

        protected override ExchangeOAuthModel Parse(string value, string format = null)
        {
            return new ExchangeOAuthModel();
        }
    }
}
