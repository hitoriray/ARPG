using System;
using System.Text;

namespace FixMath
{
	public class VectorList
    {
		protected TSVector2[] m_VertexArray;
		
		public VectorList(int count)
        {
            m_VertexArray = new TSVector2[count];
        }

        public VectorList(params TSVector2[] vertices)
        {
            int i = 0;
            m_VertexArray = new TSVector2[vertices.Length];
            foreach (TSVector2 v in vertices)
            {
                m_VertexArray[i++] = v;
            }
        }

        public int Count
        {
            get { return m_VertexArray.Length; }
        }

        public TSVector2 this[int i]
        {
            get
            {
                int idx = i % (m_VertexArray.Length);
                return m_VertexArray[idx];
            }
            set
            {
                int idx = i % (m_VertexArray.Length);
                m_VertexArray[idx] = value;
            }
        }

    }

}
