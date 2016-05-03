using System;
using System.Runtime.Serialization;

namespace seq_import
{
    class NonBindingSerializationBinder: SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            return null;
        }
    }
}
