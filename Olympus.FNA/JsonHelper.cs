using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OlympUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Olympus {
    public static class JsonHelper {


        public static readonly JsonSerializer Serializer = new() {
            Formatting = Formatting.Indented
        };

        public class ExistingCreationConverter<T> : CustomCreationConverter<T> {

            public T Value;

            public ExistingCreationConverter(T value) {
                Value = value;
            }

            public override T Create(Type objectType) {
                return Value;
            }

        }

    }
}
