using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunp.Api.Client;

namespace AgentService.References {
    public class RefOilProductType {
        public static IReadOnlyDictionary<OilProductType, string> StatusVsText => _statusVsText;
        private static readonly Dictionary<OilProductType, string> _statusVsText = new Dictionary<OilProductType, string>() {
            [OilProductType.AI76] = "АИ-76",

            [OilProductType.AI80] = "АИ-80",
            [OilProductType.AI80K2] = "АИ-80-К2",
            [OilProductType.AI80K3] = "АИ-80-К3",
            [OilProductType.AI80K4] = "АИ-80-К4",
            [OilProductType.AI80K5] = "АИ-80-К5",

            [OilProductType.AI91] = "АИ-91",
            [OilProductType.AI91K2] = "АИ-91-К2",
            [OilProductType.AI91K3] = "АИ-91-К3",
            [OilProductType.AI91K4] = "АИ-91-К4",
            [OilProductType.AI91K5] = "АИ-91-К5",

            [OilProductType.AI92] = "АИ-92",
            [OilProductType.AI92K2] = "АИ-92-К2",
            [OilProductType.AI92K3] = "АИ-92-К3",
            [OilProductType.AI92K4] = "АИ-92-К4",
            [OilProductType.AI92K5] = "АИ-92-К5",
            [OilProductType.AI92PRIME] = "Prime АИ-92",

            [OilProductType.AI93] = "АИ-93",
            [OilProductType.AI93K2] = "АИ-93-К2",
            [OilProductType.AI93K3] = "АИ-93-К3",
            [OilProductType.AI93K4] = "АИ-93-К4",
            [OilProductType.AI93K5] = "АИ-93-К5",

            [OilProductType.AI95] = "АИ-95",
            [OilProductType.AI95K2] = "АИ-95-К2", 
            [OilProductType.AI95K3] = "АИ-95-К3", 
            [OilProductType.AI95K4] = "АИ-95-К4", 
            [OilProductType.AI95K5] = "АИ-95-К5", 
            [OilProductType.AI95PREMIUM] = "Premium АИ-95",
            [OilProductType.AI95PRIME] = "Prime АИ-95", 
            [OilProductType.G95] = "G-95",

            [OilProductType.AI96] = "АИ-96",
            [OilProductType.AI96K2] = "АИ-96-К2",
            [OilProductType.AI96K3] = "АИ-96-К3",
            [OilProductType.AI96K4] = "АИ-96-К4",
            [OilProductType.AI96K5] = "АИ-96-К5",

            [OilProductType.AI98] = "АИ-98",
            [OilProductType.AI98K2] = "Аи-98-К2",
            [OilProductType.AI98K3] = "Аи-98-К3",
            [OilProductType.AI98K4] = "Аи-98-К4",
            [OilProductType.AI98K5] = "АИ-98-К5",
            [OilProductType.AI98SUPER] = "Super АИ-98",
            [OilProductType.AI98PRIME] = "Prime АИ-98",

            [OilProductType.AI100] = "АИ-100",
            [OilProductType.G100] = "G-100",

            [OilProductType.DT] = "ДТ",

            [OilProductType.DTZ] = "ДТ-З",
            [OilProductType.DTZK2] = "ДТ-З-К2",
            [OilProductType.DTZK3] = "ДТ-З-К3",
            [OilProductType.DTZK4] = "ДТ-З-К4",
            [OilProductType.DTZK5] = "ДТ-З-К5",
            [OilProductType.DTZPRIME] = "Prime ДТЗ",

            [OilProductType.DTL] = "ДТ-Л",
            [OilProductType.DTLK2] = "ДТ-Л-К2",
            [OilProductType.DTLK3] = "ДТ-Л-К3",
            [OilProductType.DTLK4] = "ДТ-Л-К4",
            [OilProductType.DTLK5] = "ДТ-Л-К5",
            [OilProductType.DTLPRIME] = "Prime ДТЛ",

            [OilProductType.DTA] = "ДТ-А",
            [OilProductType.DTAK2] = "ДТ-А-К2",
            [OilProductType.DTAK3] = "ДТ-А-К3",
            [OilProductType.DTAK4] = "ДТ-А-К4",
            [OilProductType.DTAK5] = "ДТ-А-К5",

            [OilProductType.DTE] = "ДТ-Е",
            [OilProductType.DTEK2] = "ДТ-Е-К2",
            [OilProductType.DTEK3] = "ДТ-Е-К3",
            [OilProductType.DTEK4] = "ДТ-Е-К4",
            [OilProductType.DTEK5] = "ДТ-Е-К5",

            [OilProductType.DTXP] = "ДТ-XP ", 
            [OilProductType.DTM] = "ДТ-М-К2",

            [OilProductType.M100] = "Мазут М-100",

            [OilProductType.ZM40] = "Мазут З М-40", 
            [OilProductType.ZM40] = "Мазут З М-100", 

            [OilProductType.TS1] = "Керосин ТС-1", 
            [OilProductType.JETFUEL] = "Реактивное топливо",


            [OilProductType.PTB] = "Печное Топливо Бытовое",
            [OilProductType.HYDRAZINE] = "Гидразин",

            [OilProductType.NPD] = "Неподакцизный товар",
            //[OilProductType.KotToplivo] = "Кот. топливо",
            //[OilProductType.KomponentVisokooktanBenz] = "Комп. высокооктан. бенз.", 
            //[OilProductType.Riformat] = "Риформат",
            //[OilProductType.BazaRT] = "База РТ",
            //[OilProductType.LegkiGazoilKrekinga] = "Легкий газойль крекинга",
            //[OilProductType.StKatalizat] = "Ст. катализат",
            //[OilProductType.GOBenzKK] = "ГО бензин КК",
            //[OilProductType.NaftaKrekinga] = "Нафта крекинга",
            //[OilProductType.Fr180_350] = "Фр. 180-350",
            //[OilProductType.Fr60_250] = "Фр. 60-250",
            //[OilProductType.KomponentAI92StKat] = "Компонент АИ-92 (ст.кат.)",
            [OilProductType.DISTILLYAT] = "Легкий дистиллят",
            [OilProductType.UNKNOWN] = "Не определен", 

            [OilProductType.NEFRAS] = "Нефрас",

        };
    }

    public static class RefOilProductTypeExtension {
        public static string ToDisplayText(this OilProductType status) => RefOilProductType.StatusVsText[status];
    }
}
