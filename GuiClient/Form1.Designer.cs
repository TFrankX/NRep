namespace GuiClient
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.tbServerAddress = new System.Windows.Forms.TextBox();
            this.tbServerPort = new System.Windows.Forms.TextBox();
            this.bConnect = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.bServerInfo = new System.Windows.Forms.Button();
            this.bSIMInfo = new System.Windows.Forms.Button();
            this.bAPNInfo = new System.Windows.Forms.Button();
            this.bNetworkInfo = new System.Windows.Forms.Button();
            this.bPushPowerBank = new System.Windows.Forms.Button();
            this.bPushPowerBankForce = new System.Windows.Forms.Button();
            this.bResetCabinet = new System.Windows.Forms.Button();
            this.cbSlotNumber = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tbDeviceName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.tbPass = new System.Windows.Forms.TextBox();
            this.tbLogin = new System.Windows.Forms.TextBox();
            this.bInventory = new System.Windows.Forms.Button();
            this.bSniffer = new System.Windows.Forms.Button();
            this.rtbSnif = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // rtbLog
            // 
            this.rtbLog.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.rtbLog.Location = new System.Drawing.Point(12, 162);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(776, 276);
            this.rtbLog.TabIndex = 0;
            this.rtbLog.Text = "";
            // 
            // tbServerAddress
            // 
            this.tbServerAddress.Location = new System.Drawing.Point(12, 34);
            this.tbServerAddress.Name = "tbServerAddress";
            this.tbServerAddress.Size = new System.Drawing.Size(171, 23);
            this.tbServerAddress.TabIndex = 1;
            // 
            // tbServerPort
            // 
            this.tbServerPort.Location = new System.Drawing.Point(12, 82);
            this.tbServerPort.Name = "tbServerPort";
            this.tbServerPort.Size = new System.Drawing.Size(171, 23);
            this.tbServerPort.TabIndex = 2;
            // 
            // bConnect
            // 
            this.bConnect.Location = new System.Drawing.Point(202, 115);
            this.bConnect.Name = "bConnect";
            this.bConnect.Size = new System.Drawing.Size(143, 41);
            this.bConnect.TabIndex = 6;
            this.bConnect.Text = "Коннект!";
            this.bConnect.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "Адрес сервера";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 15);
            this.label2.TabIndex = 5;
            this.label2.Text = "Порт";
            // 
            // bServerInfo
            // 
            this.bServerInfo.Location = new System.Drawing.Point(358, 12);
            this.bServerInfo.Name = "bServerInfo";
            this.bServerInfo.Size = new System.Drawing.Size(102, 23);
            this.bServerInfo.TabIndex = 7;
            this.bServerInfo.Text = "Сервер инфо";
            this.bServerInfo.UseVisualStyleBackColor = true;
            // 
            // bSIMInfo
            // 
            this.bSIMInfo.Location = new System.Drawing.Point(358, 41);
            this.bSIMInfo.Name = "bSIMInfo";
            this.bSIMInfo.Size = new System.Drawing.Size(102, 23);
            this.bSIMInfo.TabIndex = 8;
            this.bSIMInfo.Text = "SIM-card инфо";
            this.bSIMInfo.UseVisualStyleBackColor = true;
            // 
            // bAPNInfo
            // 
            this.bAPNInfo.Location = new System.Drawing.Point(358, 70);
            this.bAPNInfo.Name = "bAPNInfo";
            this.bAPNInfo.Size = new System.Drawing.Size(102, 23);
            this.bAPNInfo.TabIndex = 9;
            this.bAPNInfo.Text = "APN инфо";
            this.bAPNInfo.UseVisualStyleBackColor = true;
            // 
            // bNetworkInfo
            // 
            this.bNetworkInfo.Location = new System.Drawing.Point(358, 99);
            this.bNetworkInfo.Name = "bNetworkInfo";
            this.bNetworkInfo.Size = new System.Drawing.Size(102, 23);
            this.bNetworkInfo.TabIndex = 10;
            this.bNetworkInfo.Text = "Сеть инфо";
            this.bNetworkInfo.UseVisualStyleBackColor = true;
            // 
            // bPushPowerBank
            // 
            this.bPushPowerBank.Location = new System.Drawing.Point(485, 12);
            this.bPushPowerBank.Name = "bPushPowerBank";
            this.bPushPowerBank.Size = new System.Drawing.Size(143, 23);
            this.bPushPowerBank.TabIndex = 11;
            this.bPushPowerBank.Text = "Выдать повербанк";
            this.bPushPowerBank.UseVisualStyleBackColor = true;
            // 
            // bPushPowerBankForce
            // 
            this.bPushPowerBankForce.Location = new System.Drawing.Point(485, 41);
            this.bPushPowerBankForce.Name = "bPushPowerBankForce";
            this.bPushPowerBankForce.Size = new System.Drawing.Size(143, 23);
            this.bPushPowerBankForce.TabIndex = 12;
            this.bPushPowerBankForce.Text = "Принудительно выдать";
            this.bPushPowerBankForce.UseVisualStyleBackColor = true;
            // 
            // bResetCabinet
            // 
            this.bResetCabinet.Location = new System.Drawing.Point(485, 70);
            this.bResetCabinet.Name = "bResetCabinet";
            this.bResetCabinet.Size = new System.Drawing.Size(143, 23);
            this.bResetCabinet.TabIndex = 13;
            this.bResetCabinet.Text = "Сброс кабинета";
            this.bResetCabinet.UseVisualStyleBackColor = true;
            // 
            // cbSlotNumber
            // 
            this.cbSlotNumber.FormattingEnabled = true;
            this.cbSlotNumber.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4"});
            this.cbSlotNumber.Location = new System.Drawing.Point(648, 41);
            this.cbSlotNumber.Name = "cbSlotNumber";
            this.cbSlotNumber.Size = new System.Drawing.Size(121, 23);
            this.cbSlotNumber.TabIndex = 14;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(663, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(79, 15);
            this.label3.TabIndex = 14;
            this.label3.Text = "Номер слота";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 115);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 15);
            this.label4.TabIndex = 16;
            this.label4.Text = "Имя устройства";
            // 
            // tbDeviceName
            // 
            this.tbDeviceName.Location = new System.Drawing.Point(12, 133);
            this.tbDeviceName.Name = "tbDeviceName";
            this.tbDeviceName.Size = new System.Drawing.Size(171, 23);
            this.tbDeviceName.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(202, 64);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 15);
            this.label5.TabIndex = 20;
            this.label5.Text = "Пароль";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(202, 16);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 15);
            this.label6.TabIndex = 19;
            this.label6.Text = "Логин";
            // 
            // tbPass
            // 
            this.tbPass.Location = new System.Drawing.Point(202, 82);
            this.tbPass.Name = "tbPass";
            this.tbPass.Size = new System.Drawing.Size(143, 23);
            this.tbPass.TabIndex = 5;
            // 
            // tbLogin
            // 
            this.tbLogin.Location = new System.Drawing.Point(202, 34);
            this.tbLogin.Name = "tbLogin";
            this.tbLogin.Size = new System.Drawing.Size(143, 23);
            this.tbLogin.TabIndex = 4;
            // 
            // bInventory
            // 
            this.bInventory.Location = new System.Drawing.Point(358, 128);
            this.bInventory.Name = "bInventory";
            this.bInventory.Size = new System.Drawing.Size(102, 23);
            this.bInventory.TabIndex = 21;
            this.bInventory.Text = "Инвентарь";
            this.bInventory.UseVisualStyleBackColor = true;
            // 
            // bSniffer
            // 
            this.bSniffer.Location = new System.Drawing.Point(485, 133);
            this.bSniffer.Name = "bSniffer";
            this.bSniffer.Size = new System.Drawing.Size(143, 23);
            this.bSniffer.TabIndex = 22;
            this.bSniffer.Text = "Сниффер";
            this.bSniffer.UseVisualStyleBackColor = true;
            this.bSniffer.Click += new System.EventHandler(this.bSniffer_Click);
            // 
            // rtbSnif
            // 
            this.rtbSnif.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.rtbSnif.Location = new System.Drawing.Point(12, 162);
            this.rtbSnif.Name = "rtbSnif";
            this.rtbSnif.Size = new System.Drawing.Size(776, 276);
            this.rtbSnif.TabIndex = 23;
            this.rtbSnif.Text = "";
            this.rtbSnif.Visible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.rtbSnif);
            this.Controls.Add(this.bSniffer);
            this.Controls.Add(this.bInventory);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.tbPass);
            this.Controls.Add(this.tbLogin);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tbDeviceName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cbSlotNumber);
            this.Controls.Add(this.bResetCabinet);
            this.Controls.Add(this.bPushPowerBankForce);
            this.Controls.Add(this.bPushPowerBank);
            this.Controls.Add(this.bNetworkInfo);
            this.Controls.Add(this.bAPNInfo);
            this.Controls.Add(this.bSIMInfo);
            this.Controls.Add(this.bServerInfo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.bConnect);
            this.Controls.Add(this.tbServerPort);
            this.Controls.Add(this.tbServerAddress);
            this.Controls.Add(this.rtbLog);
            this.Name = "Form1";
            this.Text = "ChargeStation client";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private RichTextBox rtbLog;
        private TextBox tbServerAddress;
        private TextBox tbServerPort;
        private Button bConnect;
        private Label label1;
        private Label label2;
        private Button bServerInfo;
        private Button bSIMInfo;
        private Button bAPNInfo;
        private Button bNetworkInfo;
        private Button bPushPowerBank;
        private Button bPushPowerBankForce;
        private Button bResetCabinet;
        private ComboBox cbSlotNumber;
        private Label label3;
        private Label label4;
        private TextBox tbDeviceName;
        private Label label5;
        private Label label6;
        private TextBox tbPass;
        private TextBox tbLogin;
        private Button bInventory;
        private Button bSniffer;
        private RichTextBox rtbSnif;
    }
}