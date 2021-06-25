using Kentico.Kontent.Management.Modules.ModelBuilders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kentico.Kontent.Management.Tests.Data
{
    public class CustomElementProvider : IElementProvider
    {
        private Dictionary<Type, Dictionary<string, string>> _typeMap = new()
        {
            { typeof(ComplexTestModel), new()
                                        {
                                            { "ba7c8840-bcbc-5e3b-b292-24d0a60f3977", "title" },
                                            { "0827e079-3754-5a1d-9381-8ff695a5bbf7", "post_date" },
                                            { "55a88ab3-4009-5bf9-a590-f32162f09b92", "body_copy" },
                                            { "15517aa3-da8a-5551-a4d4-555461fd5226", "summary" },
                                            { "9c6a4fbc-3f73-585f-9521-8d57636adf56", "teaser_image" },
                                            { "77108990-3c30-5ffb-8dcd-8eb85fc52cb1", "related_articles" },
                                            { "1f37e15b-27a0-5f48-b314-03b401c19cee", "url_pattern" },
                                            { "c1dc36b5-558d-55a2-8f31-787430a68e4d", "personas" },
                                            { "0ee20a72-0aaa-521f-8801-df3d9293b7dd", "meta_keywords" },
                                            { "7df0048f-eaaf-50f8-85cf-fa0fc0d6d815", "meta_description" }
                                        }}
        };

        private Dictionary<Type, Dictionary<string, string>> _reverseTypeMap;

        public CustomElementProvider()
        {
            _reverseTypeMap = _typeMap.ToDictionary(x => x.Key, x => x.Value.ToDictionary((y) => y.Value, (y) => y.Key));
        }

        public string GetElementCodenameById(Type type, string id)
        {
            if (_typeMap.TryGetValue(type, out var innerMap))
            {
                if (innerMap.TryGetValue(id, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        public string GetElementIdByCodename(Type type, string name)
        {
            if (_reverseTypeMap.TryGetValue(type, out var innerMap))
            {
                if (innerMap.TryGetValue(name, out var result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}
