using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JHSNetProtocol
{
    // This can't be an interface because users don't need to implement the
    // serialization functions, we'll code generate it for them when they omit it.
    public abstract class JHSMessageBase
    {
        // De-serialize the contents of the reader into this message
        public virtual void Deserialize(JHSNetworkReader reader) { }

        // Serialize the contents of this message into the writer
        public virtual void Serialize(JHSNetworkWriter writer) { }
    }

}
