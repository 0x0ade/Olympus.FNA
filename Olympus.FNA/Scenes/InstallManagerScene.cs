using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Olympus {
    public class InstallManagerScene : Scene {

        public override bool Alert => true;

#pragma warning disable CS8618 // Generate runs before anything else.
        private Group InstallsManaged;
        private Group InstallsFound;
        private Group InstallsManual;
#pragma warning restore CS8618

        private Group InstallsFoundLoading = new Group() {
            Layout = {
                Layouts.Left(0.5f, 0),
                Layouts.Top(0.5f, 0),
                Layouts.Column(8),
            },
            Children = {
                new Spinner() {
                    Layout = { Layouts.Left(0.5f, -0.5f) },
                },
                new Label("Loading") {
                    Layout = { Layouts.Left(0.5f, -0.5f) },
                },
            }
        };

        private List<Installation> InstallsFoundAdded = new();

        public override Element Generate()
            => new Group() {
                Style = {
                    { Group.StyleKeys.Spacing, 16 },
                },
                Layout = {
                    Layouts.Fill(),
                    Layouts.Column()
                },
                Children = {

                    new HeaderBig("Celeste Installations"),

                    new Group() {
                        Clip = true,
                        ClipExtend = 16,
                        Layout = {
                            Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                        },
                        Children = {
                            new ScrollBox() {
                                Clip = false,
                                Layout = {
                                    Layouts.Fill(),
                                },
                                Content = new Group() {
                                    Style = {
                                        { Group.StyleKeys.Spacing, 16 },
                                    },
                                    Layout = {
                                        Layouts.Fill(1, 0),
                                        Layouts.Column()
                                    },
                                    Children = {

                                        new Group() {
                                            Style = {
                                                { Group.StyleKeys.Spacing, 8 },
                                            },
                                            Layout = {
                                                Layouts.Fill(1, 0, 0, 8),
                                                Layouts.Column(),
                                            },
                                            Children = {
                                                new HeaderSmall("Managed by Olympus"),
                                                new Label("TODO"),
                                            }
                                        },

                                        new Group() {
                                            Style = {
                                                { Group.StyleKeys.Spacing, 8 },
                                            },
                                            Layout = {
                                                Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                                                Layouts.Column(),
                                            },
                                            Children = {
                                                new HeaderSmall("Found on this PC"),
                                                new Group() {
                                                    Clip = true,
                                                    ClipExtend = 8,
                                                    Style = {
                                                        { Group.StyleKeys.Spacing, 8 },
                                                    },
                                                    Layout = {
                                                        Layouts.Fill(1, 0),
                                                        Layouts.Column(),
                                                    },
                                                    Init = el => InstallsFound = (Group) el,
                                                },
                                            }
                                        },

                                        new Group() {
                                            Style = {
                                                { Group.StyleKeys.Spacing, 8 },
                                            },
                                            Layout = {
                                                Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                                                Layouts.Column(),
                                            },
                                            Children = {
                                                new HeaderSmall("Manually added"),
                                                new Group() {
                                                    Clip = true,
                                                    ClipExtend = 8,
                                                    Style = {
                                                        { Group.StyleKeys.Spacing, 8 },
                                                    },
                                                    Layout = {
                                                        Layouts.Fill(1, 0),
                                                        Layouts.Column(),
                                                    },
                                                    Init = el => InstallsManual = (Group) el,
                                                },
                                            }
                                        },

                                    }
                                }
                            }
                        }
                    }

                }
            };

        public override Element PostGenerate(Element root) {
            App.Instance.FinderManager.Updated += UpdateInstallList;
            UpdateInstallList();
            return root;
        }

        private void UpdateInstallList() => UpdateInstallList(FinderUpdateState.Manual, App.Instance.FinderManager.Found);

        private void UpdateInstallList(FinderUpdateState state, List<Installation> found) {
            if (state == FinderUpdateState.Manual || state == FinderUpdateState.Start) {
                InstallsFoundAdded.Clear();
                InstallsFound.DisposeChildren();
            }

            if (state == FinderUpdateState.Start) {
                InstallsFound.Add(InstallsFoundLoading);
            }

            if (state == FinderUpdateState.Manual || state == FinderUpdateState.Add) {
                for (int i = InstallsFoundAdded.Count; i < found.Count; i++) {
                    Installation install = found[i];
                    InstallsFoundAdded.Add(install);
                    InstallsFound.Add(CreateEntry(install));
                }
            }

            if (state == FinderUpdateState.End) {
                InstallsFound.Remove(InstallsFoundLoading);
            }
        }

        private Panel CreateEntry(Installation install) {
            Label labelVersion;
            Panel panel = new() {
                Data = {
                    { "Installation", install },
                },
                Clip = true,
                Layout = {
                    Layouts.Fill(1, 0),
                    Layouts.Column(),
                },
                Style = {
                    { Group.StyleKeys.Spacing, 0 },
                },
                Children = {
                    new HeaderSmall(install.Name) {
                        Wrap = true,
                    },
                    (labelVersion = new Label("Scanning...")),
                    new LabelSmall(install.Root),
                }
            };

            Task.Run(() => {
                (bool Modifiable, string Full, Version? Version, string? Framework, string? ModName, Version? ModVersion) version = install.ScanVersion(true);
                UI.Run(() => {
                    labelVersion.Text = version.Full;
                });
            });

            return panel;
        }

    }

}
