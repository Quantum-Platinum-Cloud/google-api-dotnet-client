﻿/*
Copyright 2016 Google Inc

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using Google.Apis.Requests;
using Google.Apis.Requests.Parameters;
using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Google.Apis.Tests.Apis.Requests.Parameters
{
    /// <summary>Tests for <see cref="Google.Apis.Requests.Parameters.ParameterUtils"/>.</summary>
    public class ParameterUtilsTest
    {
        private class TestRequestUrl
        {
            [Google.Apis.Util.RequestParameterAttribute("first_query_param", 
                Google.Apis.Util.RequestParameterType.Query)]
            public string FirstParam { get; set; }

            [Google.Apis.Util.RequestParameterAttribute("second_query_param", 
                Google.Apis.Util.RequestParameterType.Query)]
            public string SecondParam { get; set; }

            [Google.Apis.Util.RequestParameterAttribute("query_param_attribute_name", 
                Google.Apis.Util.RequestParameterType.UserDefinedQueries)]
            public List<KeyValuePair<string, string>> ParamsCollection { get; set; }

            public System.Uri Build()
            {
                var builder = new RequestBuilder()
                {
                    BaseUri = new System.Uri("//revokeTokenUrl")
                };
                ParameterUtils.InitParameters(builder, this);
                return builder.BuildUri();
            }
        }

        [Fact]
        public void IterateParametersTest()
        {
            var request = new TestRequestUrl()
            {
                FirstParam = "firstOne",
                SecondParam = "secondOne",
                ParamsCollection = new List<KeyValuePair<string, string>>{
                    new KeyValuePair<string,string>("customParam1","customVal1"),
                    new KeyValuePair<string,string>("customParam2","customVal2")
                }
            };

            var result = request.Build().AbsoluteUri;

            //Parametera were given a name and a value, so both appear in the URI.
            Assert.Contains("first_query_param=firstOne", result);
            Assert.Contains("second_query_param=secondOne", result);

            //Custom parameters are key value pairs representing parameter names and values respectively.
            Assert.Contains("customParam1=customVal1", result);
            Assert.Contains("customParam2=customVal2", result);

            //The parameter name for the custom parameters does not carry through to the resulting URI.
            Assert.DoesNotContain("query_param_attribute_name", result);
        }

        private class RepeatableEnumRequest
        {
            [RequestParameter("mode", RequestParameterType.Query)]
            public FileMode? Mode { get; set; }

            [RequestParameter("mode", RequestParameterType.Query)]
            public Repeatable<FileMode> ModeList { get; set; }
        }

        [Fact]
        public void RepeatableEnum_NoPropertiesSet()
        {
            var request = new RepeatableEnumRequest();
            var dictionary = ParameterUtils.CreateParameterDictionary(request);
            // For historical reasons, we end up with a null value from the "FileMode? Mode" property.
            // That's fixable, but not worth the worry about whether it would break things.
            var pair = Assert.Single(dictionary);
            Assert.Equal("mode", pair.Key);
            Assert.Null(pair.Value);
        }

        [Fact]
        public void RepeatableEnum_SinglePropertySet()
        {
            var request = new RepeatableEnumRequest { Mode = FileMode.Open };
            var dictionary = ParameterUtils.CreateParameterDictionary(request);
            Assert.Equal(FileMode.Open, dictionary["mode"]);
        }

        [Fact]
        public void RepeatableEnum_ListPropertySet()
        {
            var request = new RepeatableEnumRequest { ModeList = new[] { FileMode.Open, FileMode.Append } };
            var dictionary = ParameterUtils.CreateParameterDictionary(request);
            var pair = Assert.Single(dictionary);
            Assert.Equal("mode", pair.Key);
            var repeatable = (Repeatable<FileMode>) pair.Value;
            Assert.Equal(new[] { FileMode.Open, FileMode.Append }, repeatable.ToArray());
        }

        [Fact]
        public void RepeatableEnum_BothPropertiesSet()
        {
            var request = new RepeatableEnumRequest
            {
                Mode = FileMode.Open,
                ModeList = new[] { FileMode.Create, FileMode.Append }
            };
            Assert.Throws<InvalidOperationException>(() => ParameterUtils.CreateParameterDictionary(request));
        }
    }
}