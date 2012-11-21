using System;
using Xpand.Persistent.Base.General.CustomAttributes;

namespace Xpand.Persistent.Base.JobScheduler.Triggers {
    public enum CronTriggerMisfireInstruction {
        InstructionNotSet,
        SmartPolicy,
        [Tooltip(@"Instructs the IScheduler that upon a mis-fire situation, the CronTrigger wants to be fired now by IScheduler. ")]
        CronTriggerFireOnceNow,
        [Tooltip(@"Instructs the IScheduler that upon a mis-fire situation, the CronTrigger wants to have it's next-fire-time updated to the next time in the schedule after the current time (taking into account any associated ICalendar, but it does not want to be fired now.")]
        CronTriggerDoNothing,
    }

    public interface IXpandCronTrigger : IXpandJobTrigger {
        string CronExpression { get; set; }
        TimeZoneId TimeZone { get; set; }
        CronTriggerMisfireInstruction MisfireInstruction { get; set; }
    }

    [Serializable]
    public enum TimeZoneId {
        Adelaide = 250,
        Afghanistan = 0xaf,
        Alaska = 3,
        Arab = 150,
        Arabian = 0xa5,
        Arabic = 0x9e,
        Atlantic = 50,
        Argentina = -2147483572,
        Armenia = -2147483574,
        Azerbaijan = -2147483584,
        Azores = 80,
        Balkan = 130,
        Cairo = 120,
        CapeVerde = 0x53,
        Caucasus = 170,
        Central = 20,
        CentralAmerica = 0x21,
        CentralAsia = 0xc3,
        CentralAustralian = 0xf5,
        CentralBrazilian = -2147483576,
        CentralCanadian = 0x19,
        CentralEurope = 0x5f,
        CentralEuropean = 100,
        CentralMexico = -2147483581,
        CentralPacific = 280,
        China = 210,
        Custom = -1,
        DateLine = 0,
        Eastern = 0x23,
        EasternAfrica = 0x9b,
        EasternAustralia = 260,
        EasternAustralian = 0xff,
        EasternEurope = 0x73,
        EasternSouthAmerica = 0x41,
        Ekaterinburg = 180,
        Fiji = 0x11d,
        Georgian = -2147483577,
        Greenland = 0x49,
        Greenwich = 90,
        Hawaii = 2,
        India = 190,
        Iran = 160,
        Israel = 0x87,
        Jordan = -2147483582,
        Kamchatka = -2147483566,
        Korea = 230,
        Lisbon = 0x55,
        Mauritius = -2147483569,
        Mexico = 30,
        Mexico2 = 13,
        MidAtlantic = 0x4b,
        MidEast = -2147483583,
        MidwayIsland = 1,
        Montevideo = -2147483575,
        Morocco = -2147483571,
        Mountain = 10,
        MountainMexico = -2147483580,
        Myanmar = 0xcb,
        Namibia = -2147483578,
        Nepal = 0xc1,
        Newfoundland = 60,
        NewZealand = 290,
        NorthAsia = 0xcf,
        NorthAsiaEast = 0xe3,
        NorthCentralAsia = 0xc9,
        NorthEurope = 0x7d,
        Pacific = 4,
        PacificMexico = -2147483579,
        Pakistan = -2147483570,
        Paraguay = -2147483567,
        Romance = 0x69,
        Russian = 0x91,
        Singapore = 0xd7,
        SouthAfrica = 140,
        SouthAmericaEastern = 70,
        SouthAmericaPacific = 0x2d,
        SouthAmericaWestern = 0x37,
        SouthEasternAsia = 0xcd,
        SouthPacific = 0x38,
        SriLanka = 200,
        Taipei = 220,
        Tasmania = 0x109,
        Tokyo = 0xeb,
        Tonga = 300,
        Ulaanbaatar = -2147483563,
        USEastern = 40,
        USMountain = 15,
        UTC = -2147483568,
        Venezuela = -2147483573,
        Vladivostok = 270,
        WestAsia = 0xb9,
        WestAustralia = 0xe1,
        WestCentralAfrica = 0x71,
        WestEurope = 110,
        WestPacific = 0x113,
        Yakutsk = 240,
        Kaliningrad = -2147483559,
        Salvador = -2147483558,
        Damascus = -2147483562
    }
}