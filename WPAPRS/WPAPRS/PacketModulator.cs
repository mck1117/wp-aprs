using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPAPRS
{
    public interface PacketModulator
    {
        float[] GetTxSamplesBuffer();
        int GetSamples();
        void PrepareToTransmit(Packet p);

    }
}
