namespace snapAssist
{
    partial class Menu
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Menu));
            button2 = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            textBox2 = new TextBox();
            label7 = new Label();
            button1 = new Button();
            SuspendLayout();
            // 
            // button2
            // 
            button2.Location = new Point(240, 185);
            button2.Margin = new Padding(3, 4, 3, 4);
            button2.Name = "button2";
            button2.Size = new Size(86, 31);
            button2.TabIndex = 1;
            button2.Text = "Acessar";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(240, 61);
            textBox1.Margin = new Padding(3, 4, 3, 4);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(182, 27);
            textBox1.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(40, 33);
            label1.Name = "label1";
            label1.Size = new Size(54, 20);
            label1.TabIndex = 3;
            label1.Text = "Meu IP";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(40, 61);
            label2.Name = "label2";
            label2.Size = new Size(97, 20);
            label2.TabIndex = 4;
            label2.Text = "11111111111";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(240, 33);
            label3.Name = "label3";
            label3.Size = new Size(156, 20);
            label3.TabIndex = 5;
            label3.Text = "Digite o IP do terceiro";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(38, 93);
            label4.Name = "label4";
            label4.Size = new Size(49, 20);
            label4.TabIndex = 6;
            label4.Text = "Senha";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(40, 121);
            label5.Name = "label5";
            label5.Size = new Size(97, 20);
            label5.TabIndex = 7;
            label5.Text = "11111111111";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(239, 103);
            label6.Name = "label6";
            label6.Size = new Size(181, 20);
            label6.TabIndex = 9;
            label6.Text = "Digite a senha do terceiro";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(239, 131);
            textBox2.Margin = new Padding(3, 4, 3, 4);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(183, 27);
            textBox2.TabIndex = 8;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(12, 240);
            label7.Name = "label7";
            label7.Size = new Size(99, 20);
            label7.TabIndex = 10;
            label7.Text = "Inicializando..";
            label7.TextAlign = ContentAlignment.TopRight;
            // 
            // button1
            // 
            button1.Location = new Point(40, 185);
            button1.Name = "button1";
            button1.Size = new Size(112, 29);
            button1.TabIndex = 11;
            button1.Text = "Copiar Senha";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Menu
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(466, 269);
            Controls.Add(button1);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(textBox2);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(button2);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            Name = "Menu";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SnapAssist";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button button2;
        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private TextBox textBox2;
        private Label label7;
        private Button button1;
    }
}