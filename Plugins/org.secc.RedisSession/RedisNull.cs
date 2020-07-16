using System;
using System.Runtime.Serialization;

namespace org.secc.RedisSession
{
    [Serializable]
    class RedisNull : ISerializable
    {
        public RedisNull()
        { }
        protected RedisNull( SerializationInfo info, StreamingContext context )
        { }
        public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
        { }
    }
}
