using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFID_Program
{
    class Buffer:Queue<TagPack>
    {
        private int volume;
        readonly static object _locker = new object();
        public Buffer(int v)
        {
            this.volume = v;
        }
        public void AddItem(TagPack item)
        {
            lock (_locker)
            {
                this.Enqueue(item);
                if (this.Count > this.volume)
                {
                    this.Dequeue();   
                }
            }
        }
        public TagPack[] GetItem()
        {
            lock (_locker)
            {
                return this.ToArray<TagPack>();
            }
        }
    }
}
