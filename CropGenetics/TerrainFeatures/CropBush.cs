﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using _SyrupFramework;
using StardewValley;

namespace Perennials
{
    public class CropBush : SoilCrop
    {

        public static Dictionary<string, Texture2D> bushSprites;

        private int seasonalSpriteRow;

        private Texture2D bushSpriteSheet;

        public CropBush() { }

        public CropBush(string cropName, int heightOffset = 0) : base(cropName, heightOffset)
        {
            Logger.Log("CropBush was created!");
            impassable = true;
            shakable = true;
            bushSpriteSheet = bushSprites[cropName];
        }

        public override void loadFromXNBData(Dictionary<string, string> cropData)
        {
            Logger.Log("Parsing as a bush crop...");
            string[] growStages = cropData["growthTimes"].Split(' ');
            foreach (string stage in growStages)
            {
                growthStages.Add(Convert.ToInt32(stage));
            }
            string[] regrowStages = cropData["matureTimes"].Split(' ');
            foreach (string stage in regrowStages)
                regrowthStages.Add(Convert.ToInt32(stage));
            if (Convert.ToBoolean(cropData["spring"]))
                seasonsToGrowIn.Add("spring");
            if (Convert.ToBoolean(cropData["summer"]))
                seasonsToGrowIn.Add("summer");
            if (Convert.ToBoolean(cropData["fall"]))
                seasonsToGrowIn.Add("fall");
            if (Convert.ToBoolean(cropData["winter"]))
                seasonsToGrowIn.Add("winter");
            perennial = true;
            tropical = Convert.ToBoolean(cropData["tropical"]);
            parseMultiHarvest(cropData["daysBetweenHarvest"]);
            rowInSpriteSheet = 0;
            columnInSpriteSheet = 0;
            parseYears(cropData["growthYears"]);
            string[] npk = cropData["npk"].Split(' ');
            nReq = Convert.ToInt32(npk[0]);
            pReq = Convert.ToInt32(npk[1]);
            kReq = Convert.ToInt32(npk[2]);
            //hydrationRequirement = (Convert.ToInt32(cropData["hydration"]) / 100);
        }

        public override Dictionary<string, string> getCropFromXNB(string data)
        {
            Dictionary<string, string> cropData = new Dictionary<string, string>();
            string[] substrings = data.Split('/');
            try
            {
                cropData["growthTimes"] = substrings[0];
                cropData["matureTimes"] = substrings[1];
                cropData["daysBetweenHarvest"] = substrings[2];
                cropData["spring"] = substrings[3];
                cropData["summer"] = substrings[4];
                cropData["fall"] = substrings[5];
                cropData["winter"] = substrings[6];
                cropData["tropical"] = substrings[7];
                cropData["growthYears"] = substrings[8];
                cropData["npk"] = substrings[9];
                Logger.Log("Parsed successfully as bush crop.");
                return cropData;
            }
            catch (IndexOutOfRangeException)
            {
                Logger.Log("Bush crop data in Bushes.xml is not in correct format!  Given\n" + data);
                return null;
            }
        }

        public override bool grow(bool hydrated, bool flooded, int xTile, int yTile, GameLocation environment, string spoofSeason = null)
        {
            string season;
            if (spoofSeason is null)
                season = Game1.currentSeason;
            else
                season = spoofSeason;

            bool grew = false;
            bool growingSeason = isGrowingSeason(spoofSeason, environment);
            string report = crop + " growth report: ";
            if (growingSeason)
            {
                report += season + " is within growing season, ";
                if (!mature)
                {
                    if (!dormant)
                    {
                        report += "was not mature, and was not dormant, ";
                        //Reset regrowMaturity, just in case.
                        regrowMaturity = 0;
                        //Progress the seed maturity by one.  When a bush reaches maturity, it immediately begins its regrowth cycle instead.
                        seedMaturity++;
                        report += "is " + seedMaturity + " days into seed growth out of " + seedDaysToMature + " needed to mature";
                        if (seedMaturity >= seedDaysToMature)
                        {
                            report += ", meaning it is now in its adult cycle. ";
                            mature = false;
                            dormant = true;
                            seedMaturity = 0;
                            regrowMaturity = 0;
                        }
                        else
                        {
                            report += ".";
                        }
                        grew = true;
                    }
                    //The bush is progressing towards fruiting.  The first phase of this growth cycle is its flowering phase.
                    //This uses the perennial "dormancy" flag, but is actually an adult yearly stage.
                    else if (dormant)
                    {
                        report += "was in its adult cycle, ";
                        regrowMaturity++;
                        report += "is " + regrowMaturity + " days into adult growth out of " + regrowDaysToMature + " needed to fruit";
                        if (regrowMaturity >= regrowDaysToMature)
                        {
                            report += ", making it ready to fruit. ";
                            mature = true;
                            dormant = false;
                        }
                        else
                        {
                            report += ".";
                        }
                        grew = true;
                    }
                }
                //New if statement, since it may have matured just now.
                if (mature)
                {
                    growFruit(season);
                    grew = true;
                }
            }
            else
            {
                report += season + " is out of its growing season, ";
                if (age != 0)
                {
                    report += "is partially grown, ";
                    //If it has fully grown, and was building towards fruiting, that is now reset.
                    if (dormant || mature)
                    {
                        report += "is in its adult growth, which is now reset. ";
                        age = 0;
                        regrowMaturity = 0;
                        dormant = true;
                        mature = false;
                        years++;
                        report += "It is now " + years + " years old.";
                    }
                    else if (!dormant && !mature)
                    {
                        report += "is still growing to maturity, and keeps its progress. ";
                    }
                }
            }
            //Logger.Log(report);
            return grew;
        }

        public override bool isSeed()
        {
            return (!dormant && !mature && getCurrentPhase() == 1);
        }

        public override void updateSpriteIndex(string spoofSeason = null)
        {
            if (!mature && !dormant)
            {
                //Still growing up, so we'll use the growing sprites.
                currentSprite = getCurrentPhase() - 2;
            }
            else
            {
                //If it's not still growing up, we are just going to use the adult sprite.
                currentSprite = 4;
            }
            string season;
            if (spoofSeason is null)
                season = Game1.currentSeason;
            else
                season = spoofSeason;
            switch (season)
            {
                case "spring":
                    seasonalSpriteRow = 0;
                    break;
                case "summer":
                    seasonalSpriteRow = 1;
                    break;
                case "fall":
                    seasonalSpriteRow = 2;
                    break;
                default:
                    seasonalSpriteRow = 3;
                    break;
            }
        }

        public override Rectangle getSprite(int number = 0)
        {
            if(isSeed())
            {
                //Returns either the top or bottom seed sprite.
                return new Rectangle(160, (number % 2) * 32, 32, 32);
            }
            //Returns the current sprite index, offset by season.
            return new Rectangle(currentSprite * 32, seasonalSpriteRow * 32, 32, 32);
        }

        public override void draw(SpriteBatch b, Vector2 tileLocation, Color toTint, float rotation)
        {
            Vector2 local = Game1.GlobalToLocal(Game1.viewport, new Vector2((float)((double)tileLocation.X * (double)Game1.tileSize) + (float)(Game1.tileSize / 2), (float)((double)tileLocation.Y * (double)Game1.tileSize) + (Game1.tileSize / 2) - (Game1.tileSize * heightOffset / 4)));
            float depth = (float)((tileLocation.Y * 64 + 32) + (tileLocation.Y * 11.0 + tileLocation.X * 7.0) % 10.0 - 5.0) / 10000f;
            
            b.Draw(bushSpriteSheet,
                local,
                getSprite((int)tileLocation.X * 7 + (int)tileLocation.Y * 11),
                toTint,
                rotation,
                new Vector2(16f, 24f),
                (float)Game1.pixelZoom,
                this.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                depth
                //(float)((tileLocation.Y * 64.0 + 32.0 + ((tileLocation.Y * 11.0 + tileLocation.X * 7.0) % 10.0 - 5.0)) / 10000.0 / ((!mature && seedMaturity <= growthStages[0]) ? 2.0 : 1.0))
            //(float)(((double)tileLocation.Y + 0.670000016689301) * 64.0 / 10000.0 + (double)tileLocation.X * 9.99999974737875E-06)
            //(float)(((double)tileLocation.Y * (double)Game1.tileSize + (double)(Game1.tileSize / 2) + (((double)tileLocation.Y * 11.0 + (double)tileLocation.X * 7.0) % 10.0 - 5.0)) / 10000.0 / (this.currentPhase != 0 || this.impassable || (specialType.Equals("Bush") && hasMatured) ? 1.0 : 2.0))
            //(float)((double)tileLocation.Y * (double)Game1.tileSize + (double)(Game1.tileSize / 2))
            );
            if (hasFruit)
            {
                //Draw the fruit overlay
                b.Draw(bushSpriteSheet,
                    local,
                    new Rectangle(160, 96, 32, 32),
                    toTint,
                    rotation,
                    new Vector2(16f, 24f),
                    (float)Game1.pixelZoom,
                    this.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    depth + (32 / 10000f)
                 //(float)(((double)tileLocation.Y * 64.0 + 32.0 + (((double)tileLocation.Y * 11.0 + (double)tileLocation.X * 7.0) % 10.0 - 5.0)) / 10000.0 / (1 - float.MinValue))
                 );
            }
            else if (!mature && dormant && age > 0 && age < regrowthStages[0])
            {
                //Draw the flower overlay
                b.Draw(bushSpriteSheet,
                    local,
                    new Rectangle(160, 64, 32, 32),
                    toTint,
                    rotation * 1.25f,
                    new Vector2(16f, 24f),
                    (float)Game1.pixelZoom,
                    this.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    depth + (32 / 10000f)
                 //(float)(((double)tileLocation.Y * 64.0 + 32.0 + (((double)tileLocation.Y * 11.0 + (double)tileLocation.X * 7.0) % 10.0 - 5.0)) / 10000.0 / (1 - float.MinValue))
                 );
            }
        }

        public override void drawInMenu(SpriteBatch b, Vector2 screenPosition, Color toTint, float rotation, float scale, float layerDepth)
        {
            b.Draw(cropSpriteSheet, screenPosition, new Rectangle?(this.getSprite(0)), toTint, rotation, new Vector2((float)(Game1.tileSize / 2), (float)(Game1.tileSize + Game1.tileSize / 2)), scale, this.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
        }


    }
}
