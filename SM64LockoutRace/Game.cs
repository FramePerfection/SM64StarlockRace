using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using StarDisplay;
using SM64AppBase;

namespace SM64LockoutRace
{
    public class Game
    {

        public class Rules
        {
            public bool sync_Keys = false;
            public bool sync_Switches = false;
            public bool sync_Cannons = true;
        }
        public Rules rules = new Rules();

        public class starAnimation
        {
            public float time = 1;
            public RectangleF rect;
            public Image image;
        }
        public starAnimation[] animations = new starAnimation[0x20 * 7];

        public static string[] specialNames = new string[] { "B1", "B2", "B3", "MC", "WC", "VC", "Sl", "OW", "S1", "S2", "S3" };
        //private static int[] specialIndex = new int[] { 0x1B, 0x1C, 0x1D, 0x21, 0x20, 0x22, 0x1E, 0x8, 0x1F, 0x23, 0x24 };
        public LayoutDescription layoutDescription = LayoutDescription.GenerateDefault();

        public int Mode = 0;
        public NetworkClient NetworkClient;
        public MemorySync memory;
        public byte[] stars = new byte[0x70];
        public byte[] myStars = new byte[0x70];
        public int saveFile;
        static Font starFont;
        static Font courseFont;
        static Font bonusFont;
        public bool started = false;
        public float scale_x = 1, scale_y = 1;
        private Matrix transform = new Matrix();
        public Bitmap imgStar, imgLockout, imgStarSlot;
        public string ROM_Name { get; private set; }

        public Game()
        {
            memory = new MemorySync();
            starFont = new Font(FontHelper.pfc.Families[0], 15, GraphicsUnit.Pixel);
            courseFont = new Font(FontHelper.pfc.Families[0], 12, GraphicsUnit.Pixel);
            bonusFont = new Font(FontHelper.pfc.Families[0], 9, GraphicsUnit.Pixel);
            imgStar = Properties.Resources.star;
            imgLockout = Properties.Resources.star_Lockout;
            imgStarSlot = Properties.Resources.star_slot;
        }

        public void connect()
        {
            if (NetworkClient == null) return;
            NetworkClient.SetMessageListener((byte)1, synchronizeGame);
            NetworkClient.SetMessageListener((byte)2, synchronizeGameStart);
            NetworkClient.SetMessageListener((byte)3, resetInternal);
            NetworkClient.WelcomeMessage = synchronizeGameStart;
            NetworkClient.GetWelcomeBuffer = () => { return stars; };
        }

        bool validateState()
        {
            short chkValue = (short)BitConverter.ToInt16(memory.ReadMemory(0x207690 + 0x36 + saveFile * 0x70, 2), 0);
            if (chkValue != 0x4441)
                return false;
            short levelID = (short)(BitConverter.ToInt16(memory.ReadMemory(0x33BAC4, 2), 0) - 1);
            int levelIndex = (levelID >= 0 ? (levelID >> 2 << 2) + 3 - levelID % 4 : -1);
            if (levelIndex + 0xC > stars.Length) return false;
            return true;
        }

        public void visualUpdate(float fTime)
        {
            if (NetworkClient != null)
            {
                NetworkClient.update(fTime);
                if (NetworkClient.ErrorText != "")
                {
                    started = false;
                    NetworkClient = null;
                    System.Windows.Forms.MessageBox.Show("Connection has been terminated.");
                }
            }

            for (int i = 0; i < 26; i++)
                for (int k = 0; k < 7; k++)
                {
                    starAnimation anim = animations[i * 7 + k];
                    if (anim != null)
                    {
                        anim.time -= fTime;
                        if (anim.time <= 0) animations[i * 7 + k] = null;
                    }
                }
        }

        public void update()
        {
            memory.Update();

            int newSaveFile = BitConverter.ToInt16(memory.ReadMemory(0x32DDF6, 2), 0);
            newSaveFile = 1;
            if (newSaveFile != saveFile)
            {
                saveFile = newSaveFile;
                if (started)
                    synchronizeGame(stars);
            }

            bool changed = false;
            byte[] newStars = memory.ReadMemory(0x207690 + saveFile * 0x70, 0x70);
            if (newStars[0x37] == 0x44 && newStars[0x36] == 0x41)
            {
                updateStarAnimations(newStars);
                for (int i = 0; i < newStars.Length; i++)
                    if (newStars[i] != stars[i])
                    {
                        changed = true;
                        if (started)
                        {
                            myStars[i] ^= (byte)(newStars[i] ^ stars[i] & newStars[i]);
                            stars[i] = (byte)(stars[i] | newStars[i]);
                        }
                        else
                            stars[i] ^= (byte)(stars[i] | (newStars[i] ^ stars[i] & newStars[i]));
                    }
                if (changed)
                {
                    stars = newStars;
                    if (NetworkClient != null && validateState())
                        NetworkClient.send(synchronizeGame, newStars);
                }
            }
            updateStarImages();

            if (started)
            {
                memory.WriteMemory(0x33b218, BitConverter.GetBytes(Mode == 0 ? totalStarCount(myStars) : totalStarCount(stars)));
                memory.WriteMemory(0x32DDF6, BitConverter.GetBytes((short)1));
            }
        }

        public void synchronizeGameStart(byte[] newStars)
        {
            started = true;
            synchronizeGame(newStars);
        }

        public void synchronizeGame(byte[] newStars)
        {
            if (newStars.Length == 0) return;

            newStars[0xA] &= 0xF;
            if (!rules.sync_Keys)
                newStars[8] &= 0xF0;
            if (!rules.sync_Switches)
                newStars[8] &= 0x8F;

            if (!rules.sync_Cannons)
            {
                for (int i = 0xC; i < 0x24; i++)
                    newStars[i] &= 0x7F;
            }

            if (Mode == 0)
                for (int i = 0; i < newStars.Length; i++)
                    stars[i] |= newStars[i];
            else
                stars = newStars;

            updateStarAnimations(newStars);
            if (!validateState()) return;
            memory.WriteMemory(0x207690 + saveFile * 0x70, stars);
            UpdateStarGeoLayouts(newStars);
        }

        void UpdateStarGeoLayouts(byte[] stars)
        {
            int geoLayoutOffset = BitConverter.ToInt32(memory.ReadMemory(0x32DDC4, 4), 0) & 0x00FFFFFF;
            int shadowStarLayout = BitConverter.ToInt32(memory.ReadMemory(geoLayoutOffset + 4 * 0x79, 4), 0);
            int yellowStarLayout = BitConverter.ToInt32(memory.ReadMemory(geoLayoutOffset + 4 * 0x7A, 4), 0);
            int currentNode = 0x33D488;
            int finalNode = BitConverter.ToInt32(memory.ReadMemory(currentNode + 0x4, 4), 0) & 0x00FFFFFF;
            uint bank_0x13 = BitConverter.ToUInt32(memory.ReadMemory(0x33B400 + 0x13 * 4, 4), 0) + 0x80000000;
            int levelID = (int)BitConverter.ToInt16(memory.ReadMemory(0x33BAC4, 2), 0) - 1;
            int levelIndex = (levelID >= 0 ? (levelID >> 2 << 2) + 3 - levelID % 4 : -1);
            int actSelectorStar = 0;
            do
            {
                uint behaviour = BitConverter.ToUInt32(memory.ReadMemory(currentNode + 0x20C, 4), 0);
                byte bParam = memory.ReadMemory(currentNode + 0x18B, 1)[0];
                byte compareByte = stars[0xB];
                if (levelID > -1) compareByte = stars[levelIndex + 0xC];
                if ((behaviour == bank_0x13 + 0x3E3C) || (behaviour == bank_0x13 + 0x7F8) || (behaviour == bank_0x13 + 0x3E64) || (behaviour == bank_0x13 + 0x80C))
                    if ((compareByte & (0x1 << bParam)) > 0)
                        memory.WriteMemory(currentNode + 0x14, BitConverter.GetBytes(shadowStarLayout));
                    else
                        memory.WriteMemory(currentNode + 0x14, BitConverter.GetBytes(yellowStarLayout));

                if (behaviour == bank_0x13 + 0x302C)
                {
                    if ((stars[levelIndex + 0xC] & (0x1 << actSelectorStar)) > 0)
                        memory.WriteMemory(currentNode + 0x14, BitConverter.GetBytes(yellowStarLayout));
                    else
                        memory.WriteMemory(currentNode + 0x14, BitConverter.GetBytes(shadowStarLayout));
                    actSelectorStar += 1;
                }

                currentNode = BitConverter.ToInt32(memory.ReadMemory(currentNode + 0x8, 4), 0) & 0x00FFFFFF; //go to next node
            }
            while (currentNode != finalNode);
        }

        private void updateStarAnimations(byte[] newStars)
        {
            for (int i = 0; i < 26; i++)
            {
                int index = i + 0xB;
                if (i >= 15) index = layoutDescription.secretDescription[i - 15].offset + 8;
                index = (index >> 2 << 2) + (3 - index % 4);
                byte newStar = (byte)(newStars[index] << 1);
                byte collectedStar = (byte)(stars[index] << 1);
                for (int k = 0; k < 7; k++)
                {
                    starAnimation anim = animations[i * 7 + k];
                    if (((collectedStar << k) & 0x80) == 0 && ((newStar << k) & 0x80) > 0)
                    {
                        anim = new starAnimation();
                        animations[i * 7 + k] = anim;
                    }
                }
            }
        }

        public void reset()
        {
            byte rulesByte = (byte)((rules.sync_Keys ? 0x1 : 0) | (rules.sync_Switches ? 0x2 : 0) | (rules.sync_Cannons ? 0x4 : 0));
            NetworkClient.send(resetInternal, new byte[] { rulesByte });
            resetInternal(new byte[] { rulesByte });
        }

        private void resetInternal(byte[] buffer)
        {
            byte rulesByte = buffer[0];
            rules.sync_Keys = (rulesByte & 0x1) > 0;
            rules.sync_Switches = (rulesByte & 0x2) > 0;
            rules.sync_Cannons = (rulesByte & 0x4) > 0;

            stars = new byte[0x70];
            stars[0x37] = 0x44;
            stars[0x36] = 0x41;
            synchronizeGame(stars);
            myStars = (byte[])stars.Clone();
            started = true;
            NetworkClient.started = true;
        }

        public int totalStarCount(byte[] starArray)
        {
            int result = 0;
            for (int i = 0; i < layoutDescription.courseDescription.Length; i++)
            {
                if (layoutDescription.courseDescription[i] == null) continue;
                int index = i + 0xB;
                index = (index >> 2 << 2) + (3 - index % 4);
                result += CountStars(starArray[index], layoutDescription.courseDescription[i].starMask);
            }
            for (int i = 0; i < layoutDescription.secretDescription.Length; i++)
                if (layoutDescription.secretDescription[i] != null)
                {
                    int index = layoutDescription.secretDescription[i].offset + 8;
                    index = (index >> 2 << 2) + (3 - index % 4);
                    result += CountStars(starArray[index], layoutDescription.secretDescription[i].starMask);
                }
            return result;
        }

        public static int CountStars(byte value, byte mask)
        {
            int v = 0;
            value &= mask;
            for (int i = 0; i < 7; i++)
            {
                v += value & 1;
                value = (byte)(value >> 1);
            }
            return v;
        }

        private void updateStarImages()
        {
            int bank0x2 = BitConverter.ToInt32(memory.ReadMemory(0x33b408, 4), 0);
            if (bank0x2 != 0)
            {
                if (imgStar.Width != 16)
                {
                    imgStar = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    imgLockout = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    imgStarSlot = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                }

                byte[] starImage = memory.ReadMemory(bank0x2 + 0x4800, 0x200);
                byte[] outputImage = new byte[starImage.Length * 2];
                byte[] outputImageTransformed = new byte[outputImage.Length];
                byte[] outputImageGrey = new byte[outputImage.Length];
                for (int y = 0; y < 0x10; y++)
                    for (int x = 0; x < 0x10; x++)
                    {
                        int i = (x + y * 0x10) * 2;
                        int b1Index = ((i / 4) * 4) + (3 - i % 4);
                        byte b1 = starImage[b1Index], b2 = starImage[b1Index - 1];
                        int outputOffset = (x + y * 0x10) * 4;
                        byte A = (byte)((b2 & 0x1) > 0 ? 255 : 0), R = (byte)(b1 & 0xF8), G = (byte)((b1 & 0x7) << 5 | ((b1 & 0xc0) >> 6)), B = (byte)((b2 & 0x3E) << 2);
                        byte v = (byte)((R + G + B) / 3);
                        outputImage[outputOffset + 3] = A; //A
                        outputImage[outputOffset + 2] = R; //R
                        outputImage[outputOffset + 1] = G; //G
                        outputImage[outputOffset + 0] = B; //B

                        outputImageTransformed[outputOffset + 3] = A; //A
                        outputImageTransformed[outputOffset + 2] = (byte)(127 - (R >> 1)); //R
                        outputImageTransformed[outputOffset + 1] = (byte)(127 - (G >> 1)); //G
                        outputImageTransformed[outputOffset + 0] = (byte)(127 - (B >> 1)); //B

                        outputImageGrey[outputOffset + 3] = (byte)((b2 & 0x1) > 0 ? 255 : 0); //A
                        outputImageGrey[outputOffset + 2] = v; //Grey
                        outputImageGrey[outputOffset + 1] = v; //Grey
                        outputImageGrey[outputOffset + 0] = v; //Grey
                    }

                BitmapData asfas = imgStar.LockBits(new Rectangle(0, 0, 16, 16), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(outputImage, 0, asfas.Scan0, outputImage.Length);
                imgStar.UnlockBits(asfas);

                asfas = imgLockout.LockBits(new Rectangle(0, 0, 16, 16), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(outputImageTransformed, 0, asfas.Scan0, outputImageTransformed.Length);
                imgLockout.UnlockBits(asfas);

                asfas = imgStarSlot.LockBits(new Rectangle(0, 0, 16, 16), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(outputImageGrey, 0, asfas.Scan0, outputImageGrey.Length);
                imgStarSlot.UnlockBits(asfas);
            }
            else
            {
                imgStar = Properties.Resources.star;
                imgLockout = Properties.Resources.star_Lockout;
                imgStarSlot = Properties.Resources.star_slot;
            }
        }

        void drawArray(Graphics g, List<starAnimation> animationList, LineDescription[] lines, int x, int y, bool secretCourses)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null) continue;
                int line = i;
                int index = (i - 1) + 0xC;
                byte myStar = 0, collectedStar = 0;
                if (secretCourses)
                    index = lines[i].offset + 8;
                index = (index >> 2 << 2) + (3 - index % 4);
                if (index >= 0)
                {
                    myStar = (byte)(myStars[index] << 1);
                    collectedStar = (byte)(stars[index] << 1);
                }

                g.DrawString(lines[i].text, bonusFont, Brushes.White, (lines[i].isTextOnly ? -15 : -20) + x, y + line * 15);
                for (int k = 0; k < 7; k++)
                {
                    if (((lines[i].starMask << k) & 0x80) != 0)
                    {
                        starAnimation anim = animations[(secretCourses ? i + 15 : i) * 7 + k];
                        if (anim != null)
                        {
                            float d = anim.time * anim.time;
                            d = d * d * 24;
                            anim.rect = new RectangleF((7 - k) * 12 + x - d, i * 15 + y - d, 12 + d * 2, 12 + d * 2);
                            anim.image = ((myStar & 0x80) > 0 || (this.Mode == 1 && (collectedStar & 0x80) > 0)) ? imgStar : imgLockout;
                            animationList.Add(anim);
                        }
                        else
                        {
                            Rectangle rect;
                            rect = new Rectangle((7 - k) * 12 + x, i * 15 + y, 12, 12);

                            if ((myStar & 0x80) > 0 || (this.Mode == 1 && (collectedStar & 0x80) > 0))
                                g.DrawImage(imgStar, rect);
                            else if ((collectedStar & 0x80) > 0)
                                g.DrawImage(imgLockout, rect);
                            else
                                g.DrawImage(imgStarSlot, rect);
                        }
                    }
                    myStar = (byte)(myStar << 1);
                    collectedStar = (byte)(collectedStar << 1);
                }
            }
        }

        public void draw(Graphics g)
        {
            transform.Reset();
            transform.Scale(scale_x, scale_y);
            g.Transform = transform;

            if (Mode == 0)
            {
                g.DrawString("Stars: " + totalStarCount(myStars), starFont, Brushes.White, 0, 10);
                g.DrawString("Total: " + totalStarCount(stars), starFont, Brushes.White, 140, 10);
            }
            else if (Mode == 1)
                g.DrawString("Total Stars: " + totalStarCount(stars), starFont, Brushes.White, 0, 10);
            int y_offset = 40;

            List<starAnimation> animationList = new List<starAnimation>();
            drawArray(g, animationList, layoutDescription.courseDescription, 20, y_offset, false);
            drawArray(g, animationList, layoutDescription.secretDescription, 145, y_offset, true);

            foreach (starAnimation anim in animationList)
                g.DrawImage(anim.image, anim.rect);
        }
    }
}
