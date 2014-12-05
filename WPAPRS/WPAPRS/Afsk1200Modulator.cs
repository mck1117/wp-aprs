/*
 * Audio FSK modem for AX25 (1200 Baud, 1200/2200Hz).
 * 
 * Copyright (C) Sivan Toledo, 2012
 * 
 *      This program is free software; you can redistribute it and/or modify
 *      it under the terms of the GNU General Public License as published by
 *      the Free Software Foundation; either version 2 of the License, or
 *      (at your option) any later version.
 *
 *      This program is distributed in the hope that it will be useful,
 *      but WITHOUT ANY WARRANTY; without even the implied warranty of
 *      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *      GNU General Public License for more details.
 *
 *      You should have received a copy of the GNU General Public License
 *      along with this program; if not, write to the Free Software
 *      Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

using System;

namespace WPAPRS
{
    public class Afsk1200Modulator : PacketModulator
    //implements HalfduplexSoundcardClient 
    {

        private float phase_inc_f0, phase_inc_f1;
        private float phase_inc_symbol;

        //private Packet packet; // received packet
        private int sample_rate;

        public Afsk1200Modulator(int sample_rate)
        {
            this.sample_rate = sample_rate;
            phase_inc_f0 = (float)(2.0 * Math.PI * 1200.0 / sample_rate);
            phase_inc_f1 = (float)(2.0 * Math.PI * 2200.0 / sample_rate);
            phase_inc_symbol = (float)(2.0 * Math.PI * 1200.0 / sample_rate);
        }

        //private float phase_f0, phase_f1;	
        //private int t; // running sample counter

        //private float f1cos, f1sin, f0cos, f0sin;

        /**************************/
        /*** Packet Transmitter ***/
        /**************************/

        public void setTxDelay(int delay) { txDelay = delay; }
        public void setTxTail(int delay) { txTail = delay; }

        private enum TxState
        {
            IDLE,
            PREAMBLE,
            DATA,
            TRAILER
        };
        private TxState txState = TxState.IDLE;
        private byte[] txBytes;
        private int txIdx;
        private int txDelay = 20; // default is 20*10ms = 500ms
        private int txTail = 0;  // obsolete
        private float txSymbolPhase, txDdsPhase;

        private float[] txSamples;
        private int txLastSymbol;
        private int txBitStuffCount;

        public void PrepareToTransmitFlags(int seconds)
        {
            if (txState != TxState.IDLE)
            {
                System.Diagnostics.Debug.WriteLine("Warning: trying to trasmit while Afsk1200 modulator is busy, discarding");
                return;
            }
            txBytes = null; // no data
            txState = TxState.PREAMBLE;
            txIdx = (int)Math.Ceiling((double)seconds / (8.0 / 1200.0)); // number of flags to transmit
            //if (transmit_controller!=null) transmit_controller.startTransmitter();
            txSymbolPhase = txDdsPhase = 0.0f;
        }

        public void PrepareToTransmit(Packet p)
        {
            if (txState != TxState.IDLE)
            {
                System.Diagnostics.Debug.WriteLine("Warning: trying to trasmit while Afsk1200 modulator is busy, discarding");
                return;
            }
            txBytes = p.BytesWithCRC(); // This includes the CRC
            txState = TxState.PREAMBLE;
            txIdx = (int)Math.Ceiling(txDelay * 0.01 / (8.0 / 1200.0)); // number of flags to transmit
            if (txIdx < 1) txIdx = 1;
            //if (transmit_controller!=null) transmit_controller.startTransmitter();
            txSymbolPhase = txDdsPhase = 0.0f;
        }

        public float[] GetTxSamplesBuffer()
        {
            if (txSamples == null)
            {
                // each byte makes up to 10 symbols,
                // each symbol takes (1/1200)s to transmit.
                // not sure if it's really necessary to add one.		
                txSamples = new float[(int)Math.Ceiling((10.0 / 1200.0) * sample_rate) + 1];
            }
            return txSamples;
        }

        private int GenerateSymbolSamples(int symbol, float[] s, int position)
        {
            int count = 0;
            while (txSymbolPhase < (float)(2.0 * Math.PI))
            {
                s[position] = (float)Math.Sin(txDdsPhase);

                if (symbol == 0) txDdsPhase += phase_inc_f0;
                else txDdsPhase += phase_inc_f1;

                txSymbolPhase += phase_inc_symbol;

                //if (tx_symbol_phase > (float) (2.0*Math.PI)) tx_symbol_phase -= (float) (2.0*Math.PI);
                if (txDdsPhase > (float)(2.0 * Math.PI)) txDdsPhase -= (float)(2.0 * Math.PI);

                position++;
                count++;
            }

            txSymbolPhase -= (float)(2.0 * Math.PI);

            return count;
        }

        private int ByteToSymbols(int bits, bool stuff)
        {
            int symbol;
            int position = 0;
            int n;
            //System.out.printf("byte=%02x stuff=%b\n",bits,stuff);
            for (int i = 0; i < 8; i++)
            {
                int bit = bits & 1;
                //System.out.println("i="+i+" bit="+bit);
                bits = bits >> 1;
                if (bit == 0)
                { // we switch sybols (frequencies)
                    symbol = (txLastSymbol == 0) ? 1 : 0;
                    n = GenerateSymbolSamples(symbol, txSamples, position);
                    position += n;

                    if (stuff) txBitStuffCount = 0;
                    txLastSymbol = symbol;
                }
                else
                {
                    symbol = (txLastSymbol == 0) ? 0 : 1;
                    n = GenerateSymbolSamples(symbol, txSamples, position);
                    position += n;

                    if (stuff) txBitStuffCount++;
                    txLastSymbol = symbol;

                    if (stuff && txBitStuffCount == 5)
                    {
                        // send a zero
                        //System.out.println("stuffing a zero bit!");
                        symbol = (txLastSymbol == 0) ? 1 : 0;
                        n = GenerateSymbolSamples(symbol, txSamples, position);
                        position += n;

                        txBitStuffCount = 0;
                        txLastSymbol = symbol;
                    }
                }
            }
            //System.out.println("generated "+position+" samples");
            return position;
        }

        public int GetSamples()
        {
            int count;

            assert(txSamples != null);

            switch (txState)
            {
                case TxState.IDLE:
                    return 0;
                case TxState.PREAMBLE:
                    count = ByteToSymbols(0x7E, false);

                    txIdx--;
                    if (txIdx == 0)
                    {
                        txState = TxState.DATA;
                        txIdx = 0;
                        txBitStuffCount = 0;
                    }
                    break;
                case TxState.DATA:
                    if (txBytes == null)
                    { // we just wanted to transmit tones to adjust the transmitter
                        txState = TxState.IDLE;
                        //if (transmit_controller!=null) transmit_controller.stopTransmitter();
                        return 0;
                    }
                    //System.out.printf("Data byte %02x\n",tx_bytes[tx_index]);
                    count = ByteToSymbols(txBytes[txIdx], true);

                    txIdx++;
                    if (txIdx == txBytes.Length)
                    {
                        txState = TxState.TRAILER;
                        if (txTail <= 0)
                        { // this should be the normal case
                            txIdx = 2;
                        }
                        else
                        {
                            txIdx = (int)Math.Ceiling(txTail * 0.01 / (8.0 / 1200.0)); // number of flags to transmit
                            if (txTail < 2) txTail = 2;
                        }
                    }
                    break;
                case TxState.TRAILER:
                    count = ByteToSymbols(0x7E, false);

                    txIdx--;
                    if (txIdx == 0)
                    {
                        txState = TxState.IDLE;
                        //if (transmit_controller!=null) transmit_controller.stopTransmitter();
                    }
                    break;
                default:
                    assert(false);
                    count = -1;
                    break;
            }

            return count;
        }

        private static void assert(bool test)
        {
            if(!test)
            {
                throw new Exception("assert fail");
            }
        }
    }
}