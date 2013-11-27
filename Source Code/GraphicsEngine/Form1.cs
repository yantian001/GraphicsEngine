﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GraphicsEngine
{
	public partial class Form1 : Form
	{
		private const int goombaMoveDelay = 14;
		private const int goombaMoveAmount = 2;
		private const int frameDelay = 5;

		private const int goombaHealth = 150;
		private const int sentryHealth = 300;

		private int groundLevel;
		private Dictionary<string, SpriteBox> spriteList = new Dictionary<string, SpriteBox>();
		private int randomCount = 0;
		private Image goombaImage = Image.FromFile(@"res\goomba.png");
		private Image sentryImage = Image.FromFile(@"res\sentry.png");
		private Random rdm = new Random();
		private int towerHealth = 100;
		private int level = 1;

		private int money
		{
			get { return internalMoney; }
			set { internalMoney = value; UpdateMoneyLbl(); }
		}
		private int internalMoney = 0;

		private List<Sentry> sentryList = new List<Sentry>();
		private List<Goomba> goombaList = new List<Goomba>();

		private void Init() //This method executes at program startup on UI thread
		{
			money = 0;
			groundLevel = Size.Height - 170;

			LoadSprite("tower", Image.FromFile("res\\tower.png"), new Point(10, groundLevel - 240));
		}

		private void Form1_Shown(object sender, EventArgs e)
		{
			AddSentry(sentryHealth, 1);
			AddSentry(sentryHealth, 1);
			AddSentry(sentryHealth, 1);
			AddSentry(sentryHealth, 1);

			int toSpawn = (2 * level) + 4;
			for (int i = 0; i < toSpawn; i++)
			{
				AddGoomba(goombaHealth + (level * 4));
			}
		}

		private void FrameLoad() //This method executes constantly with an interval specified by frameDelay on seperate thread
		{
		}

		private void sentryPurchaseBtn_Click(object sender, EventArgs e)
		{
			if (money - 50 > -1)
			{
				money -= 50;
				AddSentry(sentryHealth, 1);
				DisplayMessage("You successfully bought a sentry!");
			}
			else
			{
				DisplayMessage("You need at least 50 NK Won.");
			}
		}

		private Action<Form1> pbUpdateAction = new Action<Form1>((Form1 sender) => { sender.towerHealthPb.Value = sender.towerHealth; });
		private Action<string, Form1> spriteRemoveAction = new Action<string, Form1>((string name, Form1 sender) => { sender.RemoveSprite(name); });

		private void LetCharactersTakeAction()
		{
			int goombaIndex = 0;
			int sentryIndex = 0;

			bool goombaLoop = true;
			bool sentryLoop = true;
			while (true)
			{
				#region goomba loop
				if (goombaLoop)
				{
					if (goombaIndex < goombaList.Count)
					{
						try
						{
							if (spriteList[goombaList[goombaIndex].Name].X > 400)
							{
								spriteList[goombaList[goombaIndex].Name].X -= goombaMoveAmount;
							}
							else
							{
								if (sentryList.Count != 0)
								{
									int sentryToHurt = sentryList.Count - 1;
									sentryList[sentryToHurt].Health--;

									if (sentryList[sentryToHurt].Health < 0)
									{
										Invoke(spriteRemoveAction, new object[] { sentryList[sentryToHurt].Name, this });
										sentryList[sentryToHurt].Dispose();
										sentryList.RemoveAt(sentryToHurt);
										DisplayMessage("Oh no, goombas destroyed a sentry!");
									}
								}
								else
								{
									towerHealth--;

									if (towerHealth < 0)
									{
										DisplayMessage("Your tower was destroyed; game over at level " + level + "!");
									}
									else
									{
										Invoke(pbUpdateAction, new object[] { this });
									}
								}
							}
						}
						catch { }
					}
					else
					{
						goombaLoop = false;
					}
				}
				#endregion

				#region sentry loop
				if (sentryLoop)
				{
					if (sentryIndex < sentryList.Count)
					{
						try
						{
							if (goombaList.Count != 0)
							{
								int goombaToHurt = goombaList.Count - 1;
								goombaList[goombaToHurt].Health -= sentryList[sentryIndex].Damage;

								if (goombaList[goombaToHurt].Health < 0)
								{
									Invoke(spriteRemoveAction, new object[] { goombaList[goombaToHurt].Name, this });
									goombaList[goombaToHurt].Dispose();
									goombaList.RemoveAt(goombaToHurt);
									money += 4;
								}
							}
							else
							{
								if (nextRoundBtn.Visible == false)
								{
									level++;
									Invoke(new Action(() => { nextRoundBtn.Visible = true; }));
									Invoke(new Action(() => { levelLbl.Text = "Level " + level; }));
									money += level / 2;
									DisplayMessage("Hooray, made it to level " + level + " and earned " + level / 2 + " NK Won!");
									if (rdm.Next(0, 100) == 42)
									{
										money += level;
										DisplayMessage("Jackpot! You earn double the cash, total of " + level + " NK Won!");
									}
								}
							}
						}
						catch { }
					}
					else
					{
						sentryLoop = false;
					}
				}
				#endregion

				goombaIndex++;
				sentryIndex++;

				if (!goombaLoop && !sentryLoop)
				{
					break;
				}
			}
		}

		private void nextRoundBtn_Click(object sender, EventArgs e)
		{
			Invoke(new Action(() => { nextRoundBtn.Visible = false; }));

			if (level % 50 != 0)
			{
				int toSpawn = (2 * level) + 4;
				for (int i = 0; i < toSpawn; i++)
				{
					AddGoomba(goombaHealth + (level * 4));
				}

				DisplayMessage("Now at level " + level + ", stay alert...");
			}
			else
			{
				int toSpawn = 4 * level;
				for (int i = 0; i < toSpawn; i++)
				{
					AddGoomba(goombaHealth);
				}

				DisplayMessage("~BOSS LEVEL " + level + "~");
			}
		}

		private void MouseDownHandler(object sender, MouseEventArgs e) //Fires when mouse is clicked on UI thread - remove if not needed
		{

		}

		private System.Timers.Timer messageTimer = new System.Timers.Timer();
		private Action<string, Form1> updateStatusLblAction = new Action<string, Form1>((string text, Form1 sender) => { sender.statusLbl.Text = text; });

		private void DisplayMessage(string message)
		{
			messageTimer.Stop();
			if (InvokeRequired)
			{
				Invoke(updateStatusLblAction, new object[] { message, this });
			}
			else
			{
				statusLbl.Text = message;
			}
			messageTimer.Start();
		}

		void messageTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			messageTimer.Stop();

			if (InvokeRequired)
			{
				Invoke(updateStatusLblAction, new object[] { "Goomba Defense - Ready!", this });
			}
			else
			{
				statusLbl.Text = "Goomba Defense - Ready!";
			}
		}

		private void KeyDownHandler(object sender, KeyEventArgs e) //Fires when key is clicked on UI thread - remove if not needed
		{
			if (e.KeyData == Keys.Escape)
			{
				DisplayMessage(GetSnapshotMessage());
			}
		}

		private string GetSnapshotMessage()
		{
			string toShow = "All sentry health:\r\n";
			for (int i = 0; i < sentryList.Count; i++)
			{
				toShow += i + ": " + sentryList[i].Health + "\r\n";
			}

			toShow += "\r\nAll goomba health:\r\n";
			for (int i = 0; i < goombaList.Count; i++)
			{
				toShow += i + ": " + goombaList[i].Health + "\r\n";
			}

			return toShow;
		}

		#region character add
		private void AddGoomba(int health)
		{
			Goomba g = new Goomba(GetNextSpriteName(), health);
			goombaList.Add(g);
			Invoke(invokeGoombaAction, new object[] { g.Name, new Point(Size.Width + 50, GetRandomY()), this });
		}

		private void AddSentry(int health, int damage)
		{
			Sentry s = new Sentry(GetNextSpriteName(), health, damage);
			sentryList.Add(s);
			Invoke(invokeSentryAction, new object[] { s.Name, new Point(GetRandomSentryX(), GetRandomY()), this });
		}
		#endregion

		#region Invoked Delegates
		private Action<string, Point, Form1> invokeGoombaAction = new Action<string, Point, Form1>
			((string name, Point p, Form1 sender) => { sender.LoadSprite(name, sender.goombaImage, p); });

		private Action<string, Point, Form1> invokeSentryAction = new Action<string, Point, Form1>
			((string name, Point p, Form1 sender) => { sender.LoadSprite(name, sender.sentryImage, p); });

		#endregion

		#region Number and String gens
		private int GetRandomSentryX()
		{
			return rdm.Next(170, 300);
		}

		private int GetRandomY()
		{
			return rdm.Next(150, groundLevel);
		}

		private string GetNextSpriteName()
		{
			return "s" + ++randomCount;
		}
		#endregion

		private void UpdateMoneyLbl()
		{
			if (InvokeRequired)
				Invoke(new Action(() => { moneyLbl.Text = String.Format("You have {0} NK Won", money); }));
			else
				moneyLbl.Text = String.Format("You have {0} NK Won", money);
		}

		#region Internals
		public Form1()
		{
			InitializeComponent();
			Init();

			messageTimer.Interval = 1700;

			messageTimer.Elapsed += messageTimer_Elapsed;
			MouseDown += MouseDownHandler;
			KeyDown += KeyDownHandler;
			Shown += Form1_Shown;


			Thread frameThread = new Thread(FrameThreadInit);
			frameThread.Start();

			Thread characterThread = new Thread(CharacterLifeThreadInit);
			characterThread.Start();
		}

		private void CharacterLifeThreadInit()
		{
			while (true)
			{
				LetCharactersTakeAction();
				Thread.Sleep(goombaMoveDelay);
			}
		}

		private void LoadSprite(string name, Image img, Point loc)
		{
			spriteList[name] = new SpriteBox();
			spriteList[name].Image = img;
			spriteList[name].Location = loc;
			spriteList[name].Size = img.Size;
			//Disabled by default - Winforms ain't very good with transparency...
			spriteList[name].BackColor = Color.Transparent;
			spriteList[name].MouseDown += MouseDownHandler;
			Controls.Add(spriteList[name]);
		}

		private void RemoveSprite(string name)
		{
			Controls.Remove(spriteList[name]);
			spriteList[name].MouseDown -= MouseDownHandler;
			spriteList[name].Dispose();
			spriteList.Remove(name);
		}

		private bool AreOverlappingSprites(string spName1, string spName2)
		{
			Rectangle sp1 = new Rectangle(spriteList[spName1].X, spriteList[spName1].Y, spriteList[spName1].Width, spriteList[spName1].Height);
			Rectangle sp2 = new Rectangle(spriteList[spName2].X, spriteList[spName2].Y, spriteList[spName2].Width, spriteList[spName2].Height);
			Rectangle overlapArea = Rectangle.Intersect(sp1, sp2);

			if (overlapArea.IsEmpty)
			{
				return false;
			}
			return true;
		}

		private void FrameThreadInit()
		{
			while (true)
			{
				FrameLoad();
				Thread.Sleep(frameDelay);
			}
		}

		private Point GetCursorPos()
		{
			return PointToClient(Cursor.Position);
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			Environment.Exit(0);
		}
		#endregion

		private void button2_Click(object sender, EventArgs e)
		{
			if (money - 125 > -1)
			{
				money -= 125;
				AddSentry(sentryHealth + 75, 2);
				DisplayMessage("You successfully bought a double sentry!");
			}
			else
			{
				DisplayMessage("You need at least 125 NK Won.");
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			if (money - 500 > -1)
			{
				money -= 500;
				AddSentry(sentryHealth + 100, 4);
				DisplayMessage("You successfully bought an ultra sentry!");
			}
			else
			{
				DisplayMessage("You need at least 500 NK Won.");
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (money - 750 > -1)
			{
				money -= 750;

				for (int i = 0; i < goombaList.Count; i++)
				{
					try
					{
						Invoke(spriteRemoveAction, new object[] { goombaList[i].Name, this });
						goombaList[i].Dispose();
						goombaList.RemoveAt(i);
					}
					catch { }
				}

				DisplayMessage("You successfully bought a ray gun; most goombas are dead!");
			}
			else
			{
				DisplayMessage("You need at least 750 NK Won.");
			}
		}
	}
}
