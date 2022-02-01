using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLM.helix.Util
{
    internal class SLista<T> : List<T>
    {
        public event EventHandler OnAdd;
        public event EventHandler OnRemove;
        public event EventHandler OnRemoved;
        public event EventHandler OnAdded;

        new public void Add(T item)
        {
            if(null != OnAdd)
            {
                OnAdd(item, null);
            }
            base.Add(item);
            if(null != OnAdded)
            {
                OnAdded(item, null);
            }
        }

        new public void Remove(T item)
        {
            if(null != OnRemove)
            {
                OnRemove(item, null);
            }
            base.Remove(item);
            if(null != OnRemoved)
            {
                OnRemoved(item, null);
            }
        }


    }
}
