using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Olympus {
    public class HomeScene : Scene {

        public override Element Generate()
            => new ScrollBox() {
                Layout = {
                    Layouts.Fill()
                },
                Content = new Group() {
                    H = 800,
                    Style = {
                        { "Padding", 8 }
                    },
                    Layout = {
                        Layouts.Fill(1, 0),
                        Layouts.Column()
                    },
                    Children = {
                        new Label("Test Label")
                    },
                }
            };

    }

}
