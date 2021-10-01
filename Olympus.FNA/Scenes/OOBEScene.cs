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
    public class OOBEScene : Scene {

        public override Element Generate()
            => new Group() {
                Layout = {
                    Layouts.Fill()
                },
                Children = {
                    new Icon(OlympUI.Assets.GetTexture("header_olympus")),
                    new Panel() {
                        X = 100,
                        Y = 100,
                        Children = {
                            new Label("a") {
                                Style = {
                                    OlympUI.Assets.FontMono
                                }
                            },
                            new Label("b") {
                                Style = {
                                    Color.Red
                                },
                                Layout = {
                                    e => {
                                        Element el = e.Element;
                                        Element prev = el.Siblings[-1];
                                        el.XY = new(prev.X + prev.W, prev.Y);
                                    }
                                }
                            }
                        }
                    },
                    new Panel() {
                        Layout = {
                            Layouts.Left(0.5f, -0.5f),
                            Layouts.Top(0.5f, -0.5f),
                            Layouts.Fill(0.5f, 0.5f),
                        },
                        Cached = false,
                        Children = {
                            new ScrollBox() {
                                Layout = {
                                    Layouts.Fill()
                                },
                                Content = new Panel() {
                                    H = 800,
                                    Layout = {
                                        Layouts.Fill(1, 0),
                                        Layouts.Column()
                                    },
                                    Children = {
                                        new Label("Test Label")
                                    },
                                }
                            }
                        }
                    },
                    new Panel() {
                        Layout = {
                            Layouts.Row(8),
                            Layouts.Bottom(8),
                            Layouts.Right(8),
                        },
                        Children = {
                            new Spinner() {
                                Style = { new Color(0x00, 0xad, 0xee, 0xff) }
                            },
                            new Label("bottom text") {
                                Layout = { Layouts.Top(0.5f, -0.5f) },
                                Style = { new Color(0x00, 0xad, 0xee, 0xff) }
                            }
                        }
                    },
                }
            };

    }

}
