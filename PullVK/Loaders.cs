using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using kasthack.vksharp;
using kasthack.vksharp.DataTypes.Enums;
using Newtonsoft.Json;

namespace PullVK
{
    public class Loaders
    {
        public static async Task<string> GetFriends(int[] ids, RawApi api)
        {
            var sb = new StringBuilder();
            foreach (var id in ids)
            {
                var resp = await api.Friends.GetIds(id, count: 0);
                if (resp == null)
                    continue;
                sb.Append(resp);
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public static async Task<string> GetCountries(int[] ids, RawApi api)
        {
            var sb = new StringBuilder();
            foreach (var id in ids)
            {
                var resp = await api.Database.GetCountriesById(id);
                if (resp == null)
                    continue;
                sb.Append(resp);
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public static async Task<string> GetSubscriptions(int[] ids, RawApi api)
        {
            var sb = new StringBuilder();
            foreach (var id in ids)
            {
                var resp = await api.Users.GetSubscriptions(userId: id, count: 0);
                if (resp == null)
                    continue;
                sb.Append(resp);
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public static async Task<string> GetGroupInfo(int[] ids, RawApi api)
        {
            var sb = new StringBuilder();
            foreach (var id in ids)
            {
                var resp = await api.Groups.GetById(groupId: id, fields: GroupFields.None);
                if (resp == null)
                    continue;
                sb.Append(resp);
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public static async Task<string> GetUserInfo(int[] ids, RawApi api)
        {
            return await api.Users.Get(UserFields.Everything ^ UserFields.CommonCount, NameCase.Nom, ids);
        }

        public static async Task<string> GetGroupMembers(int[] ids, RawApi api)
        {
            var sb = new StringBuilder();
            foreach (var id in ids)
            {
                string resp = string.Empty;
                try
                {
                    resp = await api.Groups.GetMembers(id, null);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                
                if (string.IsNullOrEmpty(resp))
                {
                    Debug.WriteLine("resp==null");
                    continue;
                }
                
                int totalcount = 0;
                var myitems = new List<int>();
                try
                {
                    VKResponse response = JsonConvert.DeserializeObject<VKResponse>(resp);
                    myitems.AddRange(response.Response.Items);
                    var totallen = response.Response.Count;
                    for (int i = 100; i < totallen; i+=100)
                    {
                        string newresp = string.Empty;
                        for (int tries = 0; tries < 10; tries++)
                        {
                            newresp = await api.Groups.GetMembers(id, null, null, i, 100);
                            response = JsonConvert.DeserializeObject<VKResponse>(newresp);
                            if (response != null)
                                break;
                        }
                        if(response == null)
                            throw new ArgumentException();
                        Debug.WriteLine("{0}/{1} group {2}, len {3}", i, totallen, id, response.Response.Items.Length);
                        myitems.AddRange(response.Response.Items);
                    }
                    var totalresult = new VKResponse(new VKResponse.ValueResponse(totallen, myitems.ToArray()));
                    var finalval = JsonConvert.SerializeObject(totalresult);
                    if(string.IsNullOrEmpty(finalval))
                        Debug.WriteLine("finalval empty");
                    sb.Append(finalval);
                }
                catch
                {
                    sb.Append(resp);
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }


        public static async Task<string> GetWallContent(int[] ids, RawApi api)
        {
            var sb = new StringBuilder();
            foreach (var id in ids)
            {
                var resp = await api.Wall.Get(id);
                if (resp == null)
                    continue;
                sb.Append(resp);
                sb.Append("\n");
            }
            return sb.ToString();
        }

        [JsonObject]
        public class VKResponse
        {
            public ValueResponse Response { get; set; }

            public VKResponse(ValueResponse response)
            {
                Response = response;
            }

            [JsonObject]
            public class ValueResponse
            {
                public int Count { get; set; }
                public int[] Items { get; set; }

                public ValueResponse(int count, int[] items)
                {
                    Count = count;
                    Items = items;
                }
            }
        }
    }
}
