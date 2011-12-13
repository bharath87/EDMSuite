﻿using System;

using NationalInstruments.VisaNS;

using DAQ.Environment;

namespace DAQ.HAL
{
    /// <summary>
    /// 
    /// </summary>
    public class RS232Instrument
    {
        protected SerialSession serial;
        private string address;
        protected bool connected = false;

        public RS232Instrument(String visaAddress)
        {
            this.address = visaAddress;
        }

        protected void Connect()
        {
            if (!Environs.Debug)
            {
                if (!Environs.Debug)
                {
                    serial = new SerialSession(address);
                    serial.BaudRate = 9600;
                    serial.DataBits = 8;
                    serial.StopBits = StopBitType.One;
                    serial.ReadTermination = SerialTerminationMethod.LastBit;
                }
                connected = true;
            }
        }

        protected void Disconnect()
        {
            if (!Environs.Debug) serial.Dispose();
            connected = false;
        }

        protected void Write(String command)
        {
            serial.Write(command);
        }

        protected string Read()
        {
            return serial.ReadString();
        }
    }
}
