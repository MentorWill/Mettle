﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO.Ports;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace Charter
{
    using GraphLib;

    public partial class Form1 : Form
    {
        string RxString;

        public event TagHandeler TagEvent;
        public delegate void TagHandeler(EventTag e);
 
        public Form1()
        {
            InitializeComponent();

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            cboComPort.Items.Clear();

            ArrayList items = new ArrayList();
            items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            items.Sort();
            cboComPort.Items.AddRange(items.ToArray());

            cboComPort.SelectedText = Properties.Settings.Default.COMport;

            cboBaudRate.SelectedIndex = 1;


            //foreach (Control c in this.Controls)
            //{
            //    c.Enabled = true;
           // }


            TagEvent += new TagHandeler(aGauge3.UpdateEvent);
            TagEvent += new TagHandeler(aGauge5.UpdateEvent);
            TagEvent += new TagHandeler(stateButton1.UpdateEvent);
            TagEvent += new TagHandeler(stateButton2.UpdateEvent);
            TagEvent += new TagHandeler(stateButton3.UpdateEvent);
            TagEvent += new TagHandeler(stateButton4.UpdateEvent);
            TagEvent += new TagHandeler(ioState1.UpdateEvent);
            TagEvent += new TagHandeler(ioState2.UpdateEvent);
            TagEvent += new TagHandeler(ioState3.UpdateEvent);
            TagEvent += new TagHandeler(tagText1.UpdateEvent);
            TagEvent += new TagHandeler(tagText2.UpdateEvent);
            TagEvent += new TagHandeler(tagText3.UpdateEvent);
            TagEvent += new TagHandeler(tagText4.UpdateEvent);
            TagEvent += new TagHandeler(tagText5.UpdateEvent);
            TagEvent += new TagHandeler(tagChart1.UpdateEvent);

        }



        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                try
                {
                    //SerialPortFixer.Execute(cboComPort.Text);
                    
                    serialPort1.PortName = cboComPort.Text;
                    serialPort1.BaudRate = int.Parse(cboBaudRate.Text);
                    serialPort1.DataBits = 8;
                    serialPort1.Parity = System.IO.Ports.Parity.None;
                    serialPort1.StopBits = System.IO.Ports.StopBits.One;
                    serialPort1.RtsEnable = true;
                    serialPort1.Encoding = Encoding.GetEncoding(28591); //So I can read all 8 bits from the stupid serial port
                    serialPort1.Open();
                    serialPort1.DiscardInBuffer();

                    Properties.Settings.Default.COMport = cboComPort.Text;
                    Properties.Settings.Default.BaudRate = UInt32.Parse(cboBaudRate.Text);
                    Properties.Settings.Default.Save();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Serial port; " + ex.Message, "Error!");
                }
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    String tmpstr = serialPort1.ReadLine();

                    RxString = tmpstr;
                    this.Invoke(new EventHandler(HandleMesage));
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }

        private void HandleMesage(object sender, EventArgs e)
        {
            int position = 0;

            do //may have multiple tags per line
            {
                position = ParseTags(RxString, position);
            }
            while ((position >0) && (position < RxString.Length));

            txtData.AppendText(RxString);
            txtData.AppendText("\n");
        }
        
        private int ParseTags(string instr, int offset)
        {
            int start;
            int end = instr.Length;
            int comma;
            int d;

            //Tag format is >string, string<
            start = instr.IndexOf(">", offset);

            if(start >= 0)//May be first character
            {
                end = instr.IndexOf("<", /*offset +*/ start + 1);

                if (end > 0)
                {
                    //found start and end, find the comma
                    comma = instr.IndexOf(",", start + 1);

                    if (comma > 0)
                    {
                        //set the tag recieved event
                        EventTag t = new EventTag();

                        t.Name = instr.Substring(start + 1, comma - (start + 1)).Trim();
                        t.Data = instr.Substring(comma + 1, end - (comma + 1)).Trim();

                        if (int.TryParse(t.Data, out d))
                        {
                            t.Value = d;
                            t.ValueValid = true;

                        }

                        //make sure someone is listening
                        if(null != TagEvent)
                            TagEvent(t);
                    }
                }
            }
            return end;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            //this.Invoke(new MethodInvoker(SafeSerialClose));
            SafeSerialClose();
        }


        //TODO, close the serial port in a seperate thread to prevent
        //A GUI deadlock
        private void SafeSerialClose()
        {
            Thread myThread = new System.Threading.Thread(delegate()
            {
                //Your code here

                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }
            });
            myThread.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SafeSerialClose();

            //Wait for serialPort1 port To actually close
            Thread.Sleep(200);
        }

        private void serialPort1_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            MessageBox.Show("Serial port error; " + e.ToString());
        }

        private void aGauge3_ValueInRangeChanged(object sender, AGauge.ValueInRangeChangedEventArgs e)
        {

        }

    }
}
