﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


namespace BuffergasHardwareControl
{
    static class Runner
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

          // publish the controller to the remoting system
          
        [STAThread]
        static void Main()
        {
            // instantiate the controller
            Controller controller = new Controller();

            // publish the controller to the remoting system
            TcpChannel channel = new TcpChannel(1178);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(controller, "controller.rem");

                        
            // hand over to the controller
            controller.Start();

           
           
            
        }
    }
}