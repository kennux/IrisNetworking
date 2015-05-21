using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IrisNetworking
{
    /// <summary>
    /// Iris serialization stream.
    /// This stream can read / write values from a data source or to a byte array.
    /// This can get used for view serialization.
    /// </summary>
    public class IrisStream : IDisposable
    {
        /// <summary>
        /// Returns true if the stream is currently writing.
        /// </summary>
        public bool IsWriting
        {
            get
            {
                return this.isWriting;
            }
        }
        private bool isWriting;

        /// <summary>
        /// Returns true if the end of an reading stream is reached.
        /// This will always return false on a writing stream.
        /// </summary>
        public bool EndReached
        {
            get
            {
                return !this.IsWriting && this.data.Position >= this.data.Length-1;
            }
        }

        /// <summary>
        /// The stream data which will get used for reading.
        /// </summary>
        private MemoryStream data;

        /// <summary>
        /// The iris master object.
        /// </summary>
        private IrisMaster master;

        /// <summary>
        /// Initializes a new iris stream for writing.
        /// </summary>
        public IrisStream(IrisMaster master)
        {
            this.isWriting = true;
            this.master = master;

            this.Clear();
        }

        /// <summary>
        /// Initializes a new iris stream for reading.
        /// </summary>
        public IrisStream(IrisMaster master, byte[] data)
        {
            this.isWriting = false;
            this.master = master;

            this.Clear(data);
        }

		private int DataLeft()
		{
			return this.data.Capacity - this.data.Position;
		}

        /// <summary>
        /// Clears a writing stream
        /// </summary>
        public void Clear()
        {
            if (!this.isWriting)
                throw new NotSupportedException("Tried clearing a reading socket with the writing clearer");

            this.data = new MemoryStream();
        }

        /// <summary>
        /// Clears a reading stream
        /// </summary>
        public void Clear(byte[] newData)
        {
            if (this.isWriting)
                throw new NotSupportedException("Tried clearing a writing socket with the reading clearer");

            this.data = new MemoryStream(newData);
            this.data.Position = 0;
        }

        public void Serialize(ref bool b)
        {
            byte by = (byte)(b ? 1 : 0);
            this.Serialize(ref by);

            if (!this.isWriting)
                b = by == 1 ? true : false;
        }

        /// <summary>
        /// Serializes the given byte.
        /// </summary>
        /// <param name="b"></param>
        public void Serialize(ref byte b)
        {
            if (this.IsWriting)
                this.data.WriteByte(b);
            else
                b = (byte)this.data.ReadByte();
        }

        /// <summary>
        /// Serializes the given byte array.
        /// </summary>
        /// <param name="b"></param>
        public void Serialize(ref byte[] b)
        {
            if (this.IsWriting)
            {
                this.data.Write(BitConverter.GetBytes(b.Length), 0, 4);
                this.data.Write(b, 0, b.Length);
            }
            else
            {
                byte[] lengthBuffer = new byte[4];
                this.data.Read(lengthBuffer, 0, 4);
                int len = BitConverter.ToInt32(lengthBuffer, 0);

				if (len > this.DataLeft ())
					throw new SerializationException ("Tried to deserialize an oversized array!");

                b = new byte[len];
                this.data.Read(b, 0, len);
            }
        }

        /// <summary>
        /// Serializes the given int array.
        /// </summary>
        /// <param name="b"></param>
        public void Serialize(ref int[] b)
        {
            if (this.IsWriting)
            {
                int len = b.Length;
                this.Serialize(ref len);
                for (int i = 0; i < b.Length; i++)
                    this.Serialize(ref b[i]);
            }
            else
            {
                int len = 0;
                this.Serialize(ref len);

				if (len * 4 > this.DataLeft ())
					throw new SerializationException ("Tried to deserialize an oversized array!");

                b = new int[len];
                for (int i = 0; i < b.Length; i++)
                    this.Serialize(ref b[i]);
            }
        }

        /// <summary>
        /// Serializes the given float.
        /// </summary>
        /// <param name="b"></param>
        public void Serialize(ref float f)
        {
            if (this.IsWriting)
            {
                // Write bytes
                byte[] bytes = BitConverter.GetBytes(f);
                this.data.Write(bytes, 0, bytes.Length);
            }
            else
            {
                byte[] bytes = new byte[4];
                this.data.Read(bytes, 0, 4);
                f = BitConverter.ToSingle(bytes, 0);
            }
        }

        /// <summary>
        /// Serializes the given short.
        /// </summary>
        /// <param name="b"></param>
        public void Serialize(ref short s)
        {
            if (this.IsWriting)
            {
                // Write bytes
                byte[] bytes = BitConverter.GetBytes(s);
                this.data.Write(bytes, 0, bytes.Length);
            }
            else
            {
                byte[] bytes = new byte[2];
                this.data.Read(bytes, 0, 2);
                s = BitConverter.ToInt16(bytes, 0);
            }
        }

        /// <summary>
        /// Serializes the given int.
        /// </summary>
        /// <param name="b"></param>
        public void Serialize(ref int i)
        {
            if (this.IsWriting)
            {
                // Write bytes
                byte[] bytes = BitConverter.GetBytes(i);
                this.data.Write(bytes, 0, bytes.Length);
            }
            else
            {
                byte[] bytes = new byte[4];
                this.data.Read(bytes, 0, 4);
                i = BitConverter.ToInt32(bytes, 0);
            }
        }

        /// <summary>
        /// Serializes the given long.
        /// </summary>
        /// <param name="b"></param>
        public void Serialize(ref long l)
        {
            if (this.IsWriting)
            {
                // Write bytes
                byte[] bytes = BitConverter.GetBytes(l);
                this.data.Write(bytes, 0, bytes.Length);
            }
            else
            {
                byte[] bytes = new byte[8];
                this.data.Read(bytes, 0, 8);
                l = BitConverter.ToInt64(bytes, 0);
            }
        }

        /// <summary>
        /// Serializes the given long.
        /// Standard encoding is Encoding.ASCII
        /// </summary>
        /// <param name="b"></param>
        public void Serialize(ref string s, Encoding encoding = null)
        {
            // Standard encoding.
            if (encoding == null)
                encoding = Encoding.ASCII;

            if (s == null)
                s = "";

            if (this.IsWriting)
            {
                // Write bytes
                byte[] bytes = encoding.GetBytes(s);
                byte[] lengthBytes = BitConverter.GetBytes(bytes.Length);
                this.data.Write(lengthBytes, 0, lengthBytes.Length);
                this.data.Write(bytes, 0, bytes.Length);
            }
            else
            {
                // Load length bytes
                byte[] lengthBytes = new byte[4];
                this.data.Read(lengthBytes, 0, 4);
                int length = BitConverter.ToInt32(lengthBytes, 0);

                // Load payload string
                byte[] bytes = new byte[length];
                this.data.Read(bytes, 0, length);
                s = encoding.GetString(bytes);
            }
        }

        /// <summary>
        /// Serializes the given iris serializable object.
        /// </summary>
        /// <param name="irisSerializable"></param>
        public void SerializeObject<T>(ref T irisSerializable) where T : IrisSerializable
        {
            if (irisSerializable == null)
                irisSerializable = (T) typeof(T).GetConstructor(new Type[0]).Invoke(null);

            irisSerializable.Serialize(this);
        }

        /// <summary>
        /// Serializes the given iris serializable array object.
        /// </summary>
        /// <param name="irisSerializable"></param>
        public void SerializeObject<T>(ref T[] irisSerializables) where T : IrisSerializable, new()
        {
            if (this.isWriting)
            {
                // Write length
                byte[] lengthBytes = BitConverter.GetBytes(irisSerializables.Length);
                this.data.Write(lengthBytes, 0, lengthBytes.Length);

                // Serialize
                foreach (IrisSerializable irisSerializable in irisSerializables)
                    irisSerializable.Serialize(this);
            }
            else
            {
                // Load length bytes
                byte[] lengthBytes = new byte[4];
                this.data.Read(lengthBytes, 0, 4);
                int length = BitConverter.ToInt32(lengthBytes, 0);

				if (length > this.DataLeft)
					throw new SerializationException ("Validity check for serializable type array failed");

                // Load serializables
                irisSerializables = (T[]) Array.CreateInstance(typeof(T), length);
                for (int i = 0; i < irisSerializables.Length; i++)
                {
                    irisSerializables[i] = new T();
                    irisSerializables[i].Serialize(this);
                }
            }
        }

        /// <summary>
        /// Serializes an object with an additional type serializer.
        /// 
        /// This function will only consider additional serializers which got registered in IrisNetwork.RegisterAdditionalSerializationMethod().
        /// </summary>
        /// <param name="o"></param>
        public void SerializeAdditionalType(ref object o)
        {
            if (!IrisNetwork.SerializeAdditionalObjectType(this, ref o))
                IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisStream", "Could not serialize additional type " + o + " " + o.GetType());
        }

        /// <summary>
        /// Serializes an object array with an additional type serializer.
        /// 
        /// This function will only consider additional serializers which got registered in IrisNetwork.RegisterAdditionalSerializationMethod().
        /// </summary>
        /// <param name="o"></param>
        public void SerializeAdditionalTypeArray(ref object[] o)
        {
            if (o == null && this.isWriting)
                o = new object[0];

            if (this.isWriting)
            {
                // Write length
                byte[] lengthBytes = BitConverter.GetBytes(o.Length);
                this.data.Write(lengthBytes, 0, lengthBytes.Length);
            }
            else
            {
                // Load length bytes
                byte[] lengthBytes = new byte[4];
                this.data.Read(lengthBytes, 0, 4);
                int length = BitConverter.ToInt32(lengthBytes, 0);

				if (length > this.DataLeft)
					throw new SerializationException ("Validity check for additional type array failed");

                o = new object[length];
            }

            for (int i = 0; i < o.Length; i++)
            {
                object obj = o[i];

                if (!IrisNetwork.SerializeAdditionalObjectType(this, ref obj))
                    IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisStream", "Could not serialize additional type " + o + " " + o.GetType());

                o[i] = obj;
            }
        }

        /// <summary>
        /// Gets the current stream's bytes as byte-array.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            return this.data.ToArray();
        }

        /// <summary>
        /// Resets the current stream pointer.
        /// </summary>
        public void Reset()
        {
            this.data.Position = 0;
        }

        public virtual void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disp)
        {
            if (this.data != null)
                this.data.Close();
        }
    }
}
