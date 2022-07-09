using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Olympus {
    public class WrappedGraphicsDeviceManager : IGraphicsDeviceService, IGraphicsDeviceManager, IDisposable {

            public GraphicsDeviceManager Real;

            public bool CanCreateDevice = true;
            public bool ApplyChangesOnCreateDevice = false;

            public GraphicsDevice? GraphicsDevice => ((IGraphicsDeviceService) Real).GraphicsDevice;

            public WrappedGraphicsDeviceManager(GraphicsDeviceManager real) {
                Real = real;
            }

            public event EventHandler<EventArgs> DeviceCreated {
                add => ((IGraphicsDeviceService) Real).DeviceCreated += value;
                remove => ((IGraphicsDeviceService) Real).DeviceCreated -= value;
            }

            public event EventHandler<EventArgs> DeviceDisposing {
                add => ((IGraphicsDeviceService) Real).DeviceDisposing += value;
                remove => ((IGraphicsDeviceService) Real).DeviceDisposing -= value;
            }

            public event EventHandler<EventArgs> DeviceReset {
                add => ((IGraphicsDeviceService) Real).DeviceReset += value;
                remove => ((IGraphicsDeviceService) Real).DeviceReset -= value;
            }

            public event EventHandler<EventArgs> DeviceResetting {
                add => ((IGraphicsDeviceService) Real).DeviceResetting += value;
                remove => ((IGraphicsDeviceService) Real).DeviceResetting -= value;
            }

            public void CreateDevice() {
                if (CanCreateDevice)
                    ((IGraphicsDeviceManager) Real).CreateDevice();
                if (ApplyChangesOnCreateDevice && Real is GraphicsDeviceManager gdm)
                    gdm.ApplyChanges();
            }

            public bool BeginDraw() {
                return ((IGraphicsDeviceManager) Real).BeginDraw();
            }

            public void EndDraw() {
                ((IGraphicsDeviceManager) Real).EndDraw();
            }

            public void Dispose() {
                ((IDisposable) Real).Dispose();
            }

        }
}
