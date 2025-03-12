using NLog;
using PKOP.MDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Service.Clients.RSMDB
{
    [Serializable]
    public class RSMDBProxy
    {
        private static readonly Logger Logger = LogManager.GetLogger(typeof(RSMDBProxy).Name);

        public RSMDBProxy()
        {
            var mdb = Mdb;
        }

        private static string _RSMDBConnectionString = "server=10.65.9.51\\sqlexpress";
        public static string RSMDBConnectionString
        {
            get
            {
                return _RSMDBConnectionString;
            }
            set
            {
                _RSMDBConnectionString = value;
            }
        }


        #region RSMDB stuff

        private static object syncRoot = new Object();

        private static List<string> _Tanks;
        [XmlIgnore]
        public List<string> Tanks
        {
            get
            {
                if (_Tanks == null)
                {
                    lock (syncRoot)
                    {
                        if (_Tanks == null)
                        {
                            RSPropertyModule mdlTanks = Mdb.Children["PKOP\\TransferClient\\Tanks"];
                            if (mdlTanks != null)
                            {
                                _Tanks = new List<string>();
                                foreach (RSPropertyModule mdlTnk in mdlTanks.Children)
                                {
                                    _Tanks.Add(mdlTnk.Name);//new Tank(mdlTnk));
                                }
                            }
                        }
                    }
                }
                return _Tanks;
            }
        }

        private static List<ProductMap> _Products;
        [XmlIgnore]
        public static List<ProductMap> Products
        {
            get
            {
                if (_Products == null)
                {
                    lock (syncRoot)
                    {
                        if (_Products == null)
                        {
                            RSPropertyModule mdlProducts = Mdb.Children["PKOP\\TransferClient\\Settings\\Products"];
                            if (mdlProducts != null)
                            {
                                _Products = new List<ProductMap>();
                                ProductMap pm = null;
                                foreach (RSPropertyModule mdlPrd in mdlProducts.Children)
                                {
                                    pm = new ProductMap();
                                    pm.RSProduct = ((string)mdlPrd.Name).ToLower();
                                    pm.ID = mdlPrd.ID;
                                    _Products.Add(pm);
                                }
                            }
                        }
                    }
                }
                return _Products;
            }
        }

        private static volatile RSTraceMDB mdb;

        [XmlIgnore]
        public static RSTraceMDB Mdb
        {
            get
            {
                if (mdb != null) return mdb;
                lock (syncRoot)
                {
                    if (mdb != null) return mdb;
                    var errmsg = string.Empty;
                    mdb = MDBServer.MDB(RSMDBConnectionString, ref errmsg);
                    if (errmsg != string.Empty)
                        Logger.Error("Error connect to RSMDB.\r\n {0} \r\n Connection String: {1}", errmsg, RSMDBConnectionString);
                }
                return mdb;
            }
        }

        public List<Transfer> GetTransfers(DateTime startTime, DateTime endTime)
        {
            var listTr = new List<Transfer>();
            var trs = MDBServer.Transfers();
            Dictionary<Guid, RSTransfer> trdic = null;

            if (trs != null)
                trdic = trs.GetTransfers(startTime, endTime, null, null, "TankFarmTrace");
            else
            {
                Logger.Debug("RSTransfers is nothing");
                return null;
            }

            //Производится фильтрация. Остаются все операции в которых источник является резервуаром.
            foreach (var tr in trdic.Values)
            {
                if (Tanks.Contains(tr.Source.Name))
                    listTr.Add(new Transfer(tr));
            }
            return listTr;
        }

        #endregion

    }

    public class Tank
    {
        public Tank() { }
        public Tank(RSPropertyModule tnk)
        {
            Name = tnk.Name;
        }
        public string Name { get; private set; }
    }

    public class Transfer
    {
        public Transfer() { }
        public Transfer(RSTransfer tr)
        {
            ID = tr.ID; Source = tr.Source.Name; Destination = tr.Destination.Name; StartTime = tr.StartTime; EndTime = tr.EndTime;
            var val = tr.Value as TransModuleChildren;
            if (val == null) return;

            RSPropertyModuleTrans prp = null;
            ProductMap pmap = null;

            if (!val.IsPropertyExist("Product", ref prp)) return;

            string ProductName = prp.Value as string;
            pmap = RSMDBProxy.Products.FirstOrDefault(pm => pm.RSProduct == ProductName);
            if (pmap != null)
                ProductID = pmap.ID;
        }

        public Guid ID { get; private set; }
        public string Source { get; private set; }
        public string Destination { get; private set; }
        public Guid ProductID { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
    }

    public class ProductMap
    {
        public string RSProduct { get; set; }
        public Guid ID { get; set; }
    }

}
