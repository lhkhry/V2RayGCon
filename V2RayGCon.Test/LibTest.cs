﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using V2RayGCon.Test.Resource.Resx;

using static V2RayGCon.Lib.Utils;


namespace V2RayGCon.Test
{
    [TestClass]
    public class LibTest
    {
        [DataTestMethod]
        [DataRow(0.1, 0.2, false)]
        [DataRow(0.000000001, 0.00000002, true)]
        [DataRow(0.001, 0.002, false)]
        [DataRow(-0.1, 0.1, false)]
        [DataRow(2, 2, true)]
        public void AreEqualTest(double a, double b, bool expect)
        {
            Assert.AreEqual(expect, Lib.Utils.AreEqual(a, b));
        }

        [DataTestMethod]
        [DataRow(null, null)]
        [DataRow(
            "port=4321&ip=8.7.6.5&proto=http&type=blacklist",
            "false,false,8.7.6.5,4321,false")]
        [DataRow(
            "port=5678&ip=1233.2.3.4&proto=socks&type=whitelist",
            null)]
        [DataRow(
            "port=-5678&ip=1.2.3.4&proto=socks&type=whitelist",
            null)]
        [DataRow(
            "port=5678&ip=1.2.3.4&proto=socks&type=whitelist&debug=true",
            "true,true,1.2.3.4,5678,true")]

        // url = "type,proto,ip,port,debug"
        public void GetProxyParamsFromUrlTest(string url, string expect)
        {
            var proxyParams = Lib.Utils.GetProxyParamsFromUrl(
                url == null ? null : (
                "http://localhost:3000/pac/?&"
                + url
                + "&key="
                + Lib.Utils.RandomHex(8)));

            if (expect == null)
            {
                if (proxyParams == null)
                {
                    return;
                }
                Assert.Fail();
                return;
            }

            var expParts = expect.Split(',');

            if (
                proxyParams.isWhiteList.ToString().ToLower() != expParts[0]
                || proxyParams.isSocks.ToString().ToLower() != expParts[1]
                || proxyParams.ip != expParts[2]
                || proxyParams.port.ToString() != expParts[3]
                || proxyParams.isDebug.ToString().ToLower() != expParts[4])
            {
                Assert.Fail();
            }
        }

        [DataTestMethod]
        [DataRow("11,22 2,5 3,4 7,8 1,2 6,6", "1,8 11,22")]
        [DataRow("11,22 2,5 3,4 7,8 1,2", "1,5 7,8 11,22")]
        [DataRow("1,2 3,4 1,1 1,2", "1,4")]
        public void CompactRangeArrayListTest(string org, string expect)
        {
            long[] rangeParser(string rangeArray)
            {
                var v = rangeArray.Split(',');
                return new long[] {
                    (long)Lib.Utils.Str2Int(v[0]),
                    (long)Lib.Utils.Str2Int(v[1]),
                };
            }

            List<long[]> listParser(string listString)
            {
                var r = new List<long[]>();
                foreach (var item in listString.Split(' '))
                {
                    r.Add(rangeParser(item));
                }
                return r;
            }

            var o = listParser(org);
            var e = listParser(expect);
            var result = Lib.Utils.CompactCidrList(ref o);

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i][0] != e[i][0] || result[i][1] != e[i][1])
                {
                    Assert.Fail();
                }
            }
        }

        [DataTestMethod]
        [DataRow("172.316.254.1", 2906455553)] // becareful not a valid ip
        [DataRow("0.254.255.0", 16711424)]
        [DataRow("127.0.0.1", 2130706433)]
        [DataRow("0.0.0.0", 0)]
        public void IP2Int32Test(string address, long expect)
        {
            Assert.AreEqual(expect, Lib.Utils.IP2Long(address));
        }

        [DataTestMethod]
        [DataRow("172.316.254.1", false)]
        [DataRow("0.254.255.0", true)]
        [DataRow("192.168.1.15_1", false)]
        [DataRow("127.0.0.1", true)]
        [DataRow("0.0.0.", false)]
        [DataRow("0.0.0.0", true)]
        public void IsIPTest(string address, bool expect)
        {
            Assert.AreEqual(expect, Lib.Utils.IsIP(address));
        }

        [DataTestMethod]
        [DataRow("EvABk文,tv字vvc", "字文", false)]
        [DataRow("EvABk文,tv字vvc", "ab字", true)]
        [DataRow("ab vvvc", "bc", true)]
        [DataRow("abc", "ac", true)]
        [DataRow("", "a", false)]
        [DataRow("", "", true)]
        public void PartialMatchTest(string source, string partial, bool expect)
        {
            var result = Lib.Utils.PartialMatch(source, partial);
            Assert.AreEqual(expect, result);
        }


        [TestMethod]
        public void GetFreePortTest()
        {
            int port = Lib.Utils.GetFreeTcpPort();
            Assert.AreEqual(true, port > 0);
        }

        [DataTestMethod]
        [DataRow("http://www.baidu.com")]
        public void VisitWebPageSpeedTestTest(string url)
        {
            var time = Lib.Utils.VisitWebPageSpeedTest(url);
            Assert.AreEqual(true, time < long.MaxValue);
        }

        [DataTestMethod]
        [DataRow("aaaaaa", 0, "...")]
        [DataRow("aaaaaaaaa", 5, "aa...")]
        [DataRow("aaaaaa", 3, "...")]
        [DataRow("aaaaaa", -1, "...")]
        [DataRow("", 100, "")]
        public void CutStrTest(string org, int len, string expect)
        {
            var cut = Lib.Utils.CutStr(org, len);
            Assert.AreEqual(expect, cut);
        }

        [DataTestMethod]
        [DataRow(@"{}", "")]
        [DataRow(@"{v2raygcon:{env:['1','2']}}", "")]
        [DataRow(@"{v2raygcon:{env:{a:'1',b:2}}}", "a:1,b:2")]
        [DataRow(@"{v2raygcon:{env:{a:'1',b:'2'}}}", "a:1,b:2")]
        public void GetEnvVarsFromConfigTest(string json, string expect)
        {
            var j = JObject.Parse(json);
            var env = Lib.Utils.GetEnvVarsFromConfig(j);
            var strs = env.OrderBy(p => p.Key).Select(p => p.Key + ":" + p.Value);
            var r = string.Join(",", strs);

            Assert.AreEqual(expect, r);
        }


        [TestMethod]
        public void CreateDeleteAppFolderTest()
        {
            var appFolder = Lib.Utils.GetAppDataFolder();
            Assert.AreEqual(false, string.IsNullOrEmpty(appFolder));

            // do not run these tests 
            // Lib.Utils.CreateAppDataFolder();
            // Assert.AreEqual(true, Directory.Exists(appFolder));
            // Lib.Utils.DeleteAppDataFolder();
            // Assert.AreEqual(false, Directory.Exists(appFolder));
        }

        [DataTestMethod]
        [DataRow(@"{}", "a", "abc", @"{'a':'abc'}")]
        [DataRow(@"{'a':{'b':{'c':1234}}}", "a.b.c", "abc", @"{'a':{'b':{'c':'abc'}}}")]
        public void SetValueStringTest(string json, string path, string value, string expect)
        {
            var r = JObject.Parse(json);
            var e = JObject.Parse(expect);
            Lib.Utils.SetValue<string>(r, path, value);
            Assert.AreEqual(true, JObject.DeepEquals(e, r));
        }

        [DataTestMethod]
        [DataRow(@"{}", "a", 1, @"{'a':1}")]
        [DataRow(@"{'a':{'b':{'c':1234}}}", "a.b.c", 5678, @"{'a':{'b':{'c':5678}}}")]
        public void SetValueIntTest(string json, string path, int value, string expect)
        {
            var r = JObject.Parse(json);
            var e = JObject.Parse(expect);
            Lib.Utils.SetValue<int>(r, path, value);
            Assert.AreEqual(true, JObject.DeepEquals(e, r));
        }

        [DataTestMethod]
        [DataRow(@"{'a':{'c':null},'b':1}", "a.b.c")]
        [DataRow(@"{'a':[0,1,2],'b':1}", "a.0")]
        [DataRow(@"{}", "")]
        public void RemoveKeyFromJsonFailTest(string json, string key)
        {
            // outboundDetour inboundDetour
            var j = JObject.Parse(json);
            Assert.ThrowsException<KeyNotFoundException>(() =>
            {
                RemoveKeyFromJObject(j, key);
            });
        }

        [DataTestMethod]
        [DataRow(@"{'a':{'c':null,'a':2},'b':1}", "a.c", @"{'a':{'a':2},'b':1}")]
        [DataRow(@"{'a':{'c':1},'b':1}", "a.c", @"{'a':{},'b':1}")]
        [DataRow(@"{'a':{'c':1},'b':1}", "a.b", @"{'a':{'c':1},'b':1}")]
        [DataRow(@"{'a':1,'b':1}", "c", @"{'a':1,'b':1}")]
        [DataRow(@"{'a':1,'b':1}", "a", @"{'b':1}")]
        public void RemoveKeyFromJsonNormalTest(string json, string key, string expect)
        {
            // outboundDetour inboundDetour
            var j = JObject.Parse(json);
            RemoveKeyFromJObject(j, key);
            var e = JObject.Parse(expect);
            Assert.AreEqual(true, JObject.DeepEquals(e, j));
        }

        [DataTestMethod]
        [DataRow("", "")]
        [DataRow("1", "1")]
        [DataRow("1 , 2", "1,2")]
        [DataRow(",  ,  ,", "")]
        [DataRow(",,,  ,1  ,  ,2,  ,3,,,", "1,2,3")]
        public void Str2JArray2Str(string value, string expect)
        {
            var array = Lib.Utils.Str2JArray(value);
            var str = Lib.Utils.JArray2Str(array);
            Assert.AreEqual(expect, str);
        }

        [DataTestMethod]
        [DataRow("0", 0)]
        [DataRow("-1", -1)]
        [DataRow("str-1.234", 0)]
        [DataRow("-1.234str", 0)]
        [DataRow("-1.234", -1)]
        [DataRow("1.432", 1)]
        [DataRow("1.678", 2)]
        [DataRow("-1.678", -2)]
        public void Str2Int(string value, int expect)
        {
            Assert.AreEqual(expect, Lib.Utils.Str2Int(value));
        }

        [TestMethod]
        public void GetLocalCoreVersion()
        {

            var core = new Service.Core();
            var version = core.GetCoreVersion();

            if (core.IsExecutableExist())
            {
                Assert.AreNotEqual(string.Empty, version);
            }
            else
            {
                Assert.AreEqual(string.Empty, version);
            }
        }

        [TestMethod]
        public void GetValue_GetBoolFromString_ReturnDefault()
        {
            var json = Service.Cache.Instance.
                tpl.LoadMinConfig();
            Assert.AreEqual(default(bool), GetValue<bool>(json, "log.loglevel"));
        }

        [TestMethod]
        public void GetValue_GetStringNotExist_ReturnNull()
        {
            var json = Service.Cache.Instance.
                tpl.LoadMinConfig();
            Assert.AreEqual(string.Empty, GetValue<string>(json, "log.keyNotExist"));
        }

        [TestMethod]
        public void GetValue_KeyNotExist_ReturnDefault()
        {
            var json = Service.Cache.Instance.
                tpl.LoadMinConfig();
            var value = Lib.Utils.GetValue<int>(json, "log.key_not_exist");
            Assert.AreEqual(default(int), value);
        }

        [TestMethod]
        public void ConfigResource_Validate()
        {
            foreach (var config in Lib.Utils.TestingGetResourceConfigJson())
            {
                try
                {
                    JObject.Parse(config);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        public void Str2ListStr()
        {
            var testData = new Dictionary<string, int> {
                // string serial, int expectLength
                {"",0 },
                {",,,,",0 },
                {"1.1,2.2,,3.3,,,4.4.4,", 4},
            };

            foreach (var item in testData)
            {
                var len = Lib.Utils.Str2ListStr(item.Key).Count;
                Assert.AreEqual(item.Value, len);
            }
        }

        [TestMethod]
        public void ExtractLinks_FromString()
        {
            // var content = testData("links");
            var content = "ss://ZHVtbXkwMA==";
            var links = Lib.Utils.ExtractLinks(content, Model.Data.Enum.LinkTypes.ss);
            var expact = "ss://ZHVtbXkwMA==";
            Assert.AreEqual(links.Count, 1);
            Assert.AreEqual(expact, links[0]);
        }

        [TestMethod]
        public void ExtractLinks_FromLinksTxt()
        {
            var content = TestConst.links;
            var links = Lib.Utils.ExtractLinks(content, Model.Data.Enum.LinkTypes.vmess);
            Assert.AreEqual(2, links.Count);
        }

        [TestMethod]
        public void ExtractLink_FromEmptyString_Return_EmptyList()
        {
            var content = "";
            var links = Lib.Utils.ExtractLinks(content, Model.Data.Enum.LinkTypes.vmess);
            Assert.AreEqual(0, links.Count);
        }

        [TestMethod]
        public void GetRemoteCoreVersions()
        {
            List<string> versions = Lib.Utils.GetCoreVersions();
            // Assert.AreNotEqual(versions, null);
            Assert.AreEqual(true, versions.Count > 0);
        }

        [TestMethod]
        public void GetVGCVersions()
        {
            var version = Lib.Utils.GetLatestVGCVersion();
            Assert.AreNotEqual(string.Empty, version);

        }

    }
}
