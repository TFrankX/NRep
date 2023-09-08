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
            rtbLog = new RichTextBox();
            tbServerAddress = new TextBox();
            tbServerPort = new TextBox();
            bConnect = new Button();
            label1 = new Label();
            label2 = new Label();
            bServerInfo = new Button();
            bSIMInfo = new Button();
            bAPNInfo = new Button();
            bCabinetInfo = new Button();
            bPushPowerBank = new Button();
            bPushPowerBankForce = new Button();
            bResetCabinet = new Button();
            cbSlotNumber = new ComboBox();
            label3 = new Label();
            label4 = new Label();
            tbDeviceName = new TextBox();
            label5 = new Label();
            label6 = new Label();
            tbPass = new TextBox();
            tbLogin = new TextBox();
            bInventory = new Button();
            SuspendLayout();
            // 
            // rtbLog
            // 
            rtbLog.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            rtbLog.Location = new Point(12, 162);
            rtbLog.Name = "rtbLog";
            rtbLog.Size = new Size(776, 276);
            rtbLog.TabIndex = 0;
            rtbLog.Text = "";
            rtbLog.TextChanged += rtbLog_TextChanged;
            // 
            // tbServerAddress
            // 
            tbServerAddress.Location = new Point(12, 34);
            tbServerAddress.Name = "tbServerAddress";
            tbServerAddress.Size = new Size(171, 23);
            tbServerAddress.TabIndex = 1;
            // 
            // tbServerPort
            // 
            tbServerPort.Location = new Point(12, 82);
            tbServerPort.Name = "tbServerPort";
            tbServerPort.Size = new Size(171, 23);
            tbServerPort.TabIndex = 2;
            // 
            // bConnect
            // 
            bConnect.Location = new Point(202, 115);
            bConnect.Name = "bConnect";
            bConnect.Size = new Size(143, 41);
            bConnect.TabIndex = 6;
            bConnect.Text = "Коннект!";
            bConnect.UseVisualStyleBackColor = true;
            bConnect.Click += bConnect_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 16);
            label1.Name = "label1";
            label1.Size = new Size(87, 15);
            label1.TabIndex = 4;
            label1.Text = "Адрес сервера";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 64);
            label2.Name = "label2";
            label2.Size = new Size(35, 15);
            label2.TabIndex = 5;
            label2.Text = "Порт";
            // 
            // bServerInfo
            // 
            bServerInfo.Location = new Point(358, 12);
            bServerInfo.Name = "bServerInfo";
            bServerInfo.Size = new Size(102, 23);
            bServerInfo.TabIndex = 7;
            bServerInfo.Text = "Сервер инфо";
            bServerInfo.UseVisualStyleBackColor = true;
            bServerInfo.Click += bServerInfo_Click;
            // 
            // bSIMInfo
            // 
            bSIMInfo.Location = new Point(358, 41);
            bSIMInfo.Name = "bSIMInfo";
            bSIMInfo.Size = new Size(102, 23);
            bSIMInfo.TabIndex = 8;
            bSIMInfo.Text = "SIM-card инфо";
            bSIMInfo.UseVisualStyleBackColor = true;
            // 
            // bAPNInfo
            // 
            bAPNInfo.Location = new Point(358, 70);
            bAPNInfo.Name = "bAPNInfo";
            bAPNInfo.Size = new Size(102, 23);
            bAPNInfo.TabIndex = 9;
            bAPNInfo.Text = "APN инфо";
            bAPNInfo.UseVisualStyleBackColor = true;
            // 
            // bCabinetInfo
            // 
            bCabinetInfo.Location = new Point(358, 99);
            bCabinetInfo.Name = "bCabinetInfo";
            bCabinetInfo.Size = new Size(102, 23);
            bCabinetInfo.TabIndex = 10;
            bCabinetInfo.Text = "Сеть инфо";
            bCabinetInfo.UseVisualStyleBackColor = true;
            bCabinetInfo.Click += bCabinetInfo_Click;
            // 
            // bPushPowerBank
            // 
            bPushPowerBank.Location = new Point(485, 12);
            bPushPowerBank.Name = "bPushPowerBank";
            bPushPowerBank.Size = new Size(143, 23);
            bPushPowerBank.TabIndex = 11;
            bPushPowerBank.Text = "Выдать повербанк";
            bPushPowerBank.UseVisualStyleBackColor = true;
            bPushPowerBank.Click += bPushPowerBank_Click;
            // 
            // bPushPowerBankForce
            // 
            bPushPowerBankForce.Location = new Point(485, 41);
            bPushPowerBankForce.Name = "bPushPowerBankForce";
            bPushPowerBankForce.Size = new Size(143, 23);
            bPushPowerBankForce.TabIndex = 12;
            bPushPowerBankForce.Text = "Принудительно выдать";
            bPushPowerBankForce.UseVisualStyleBackColor = true;
            // 
            // bResetCabinet
            // 
            bResetCabinet.Location = new Point(485, 70);
            bResetCabinet.Name = "bResetCabinet";
            bResetCabinet.Size = new Size(143, 23);
            bResetCabinet.TabIndex = 13;
            bResetCabinet.Text = "Сброс кабинета";
            bResetCabinet.UseVisualStyleBackColor = true;
            bResetCabinet.Click += bResetCabinet_Click;
            // 
            // cbSlotNumber
            // 
            cbSlotNumber.FormattingEnabled = true;
            cbSlotNumber.Items.AddRange(new object[] { "1", "2", "3", "4" });
            cbSlotNumber.Location = new Point(648, 41);
            cbSlotNumber.Name = "cbSlotNumber";
            cbSlotNumber.Size = new Size(121, 23);
            cbSlotNumber.TabIndex = 14;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(663, 20);
            label3.Name = "label3";
            label3.Size = new Size(79, 15);
            label3.TabIndex = 14;
            label3.Text = "Номер слота";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 115);
            label4.Name = "label4";
            label4.Size = new Size(95, 15);
            label4.TabIndex = 16;
            label4.Text = "Имя устройства";
            // 
            // tbDeviceName
            // 
            tbDeviceName.Location = new Point(12, 133);
            tbDeviceName.Name = "tbDeviceName";
            tbDeviceName.Size = new Size(171, 23);
            tbDeviceName.TabIndex = 3;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(202, 64);
            label5.Name = "label5";
            label5.Size = new Size(49, 15);
            label5.TabIndex = 20;
            label5.Text = "Пароль";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(202, 16);
            label6.Name = "label6";
            label6.Size = new Size(41, 15);
            label6.TabIndex = 19;
            label6.Text = "Логин";
            // 
            // tbPass
            // 
            tbPass.Location = new Point(202, 82);
            tbPass.Name = "tbPass";
            tbPass.Size = new Size(143, 23);
            tbPass.TabIndex = 5;
            // 
            // tbLogin
            // 
            tbLogin.Location = new Point(202, 34);
            tbLogin.Name = "tbLogin";
            tbLogin.Size = new Size(143, 23);
            tbLogin.TabIndex = 4;
            // 
            // bInventory
            // 
            bInventory.Location = new Point(358, 128);
            bInventory.Name = "bInventory";
            bInventory.Size = new Size(102, 23);
            bInventory.TabIndex = 21;
            bInventory.Text = "Инвентарь";
            bInventory.UseVisualStyleBackColor = true;
            bInventory.Click += bInventory_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(bInventory);
            Controls.Add(label5);
            Controls.Add(label6);
            Controls.Add(tbPass);
            Controls.Add(tbLogin);
            Controls.Add(label4);
            Controls.Add(tbDeviceName);
            Controls.Add(label3);
            Controls.Add(cbSlotNumber);
            Controls.Add(bResetCabinet);
            Controls.Add(bPushPowerBankForce);
            Controls.Add(bPushPowerBank);
            Controls.Add(bCabinetInfo);
            Controls.Add(bAPNInfo);
            Controls.Add(bSIMInfo);
            Controls.Add(bServerInfo);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(bConnect);
            Controls.Add(tbServerPort);
            Controls.Add(tbServerAddress);
            Controls.Add(rtbLog);
            Name = "Form1";
            Text = "Form1";
            FormClosed += Form1_FormClosed;
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
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
        private Button bCabinetInfo;
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
    }
}