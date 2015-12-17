using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;


namespace Aiv.Draw.OpenGL
{

	public enum PixelFormat
	{
		BW,
		Grayscale,
		RGB,
		RGBA,
	}


	public enum KeyCode
	{
		A = Key.A,
		B = Key.B,
		C = Key.C,
		D = Key.D,
		E = Key.E,
		F = Key.F,
		G = Key.G,
		H = Key.H,
		I = Key.I,
		J = Key.J,
		K = Key.K,
		L = Key.L,
		M = Key.M,
		N = Key.N,
		O = Key.O,
		P = Key.P,
		Q = Key.Q,
		R = Key.R,
		S = Key.S,
		T = Key.T,
		U = Key.U,
		V = Key.V,
		W = Key.W,
		X = Key.X,
		Y = Key.Y,
		Z = Key.Z,

		Space = Key.Space,
		Return = Key.Enter,
		Esc = Key.Escape,

		Up = Key.Up,
		Down = Key.Down,
		Left = Key.Left,
		Right = Key.Right,
	}

	public class Window
	{

		private GameWindow window;

		/// <summary>
		/// Used to draw into the game window
		/// </summary>
		public byte[] bitmap;

		/// <summary>
		/// Window width
		/// </summary>
		public int width;
		/// <summary>
		/// Window height
		/// </summary>
		public int height;

		/// <summary>
		/// Get or sets the cursor visibility
		/// </summary>
		public bool CursorVisible {
			set {
				window.CursorVisible = value;
			}
		}

		private PixelFormat pixelFormat;

		private Stopwatch watch;

		private float _deltaTime;

		/// <summary>
		/// Time (in seconds) passed since the last <c>Blit()</c>
		/// </summary>
		public float deltaTime {
			get {
				return _deltaTime;
			}
		}

		/// <summary>
		/// Sets or get if window is opened or closed;
		/// </summary>
		public bool opened = true;

		private KeyboardState _keyboardState;
		private MouseState _mouseState;

		private int vertexArrayId;
		private int shaderProgramId;
		private int textureId;


		/// <summary>
		/// Sets Window's Icon
		/// </summary>
		/// <param name="path">path to the icon</param>
		/// <param name="isRelative">if <c>true</c>, the path will be relative to the application location, otherwise the path will be absoulte</param>
		public void SetIcon (string path, bool isRelative)
		{
			if (isRelative)
				this.window.Icon = new Icon (AppDomain.CurrentDomain.BaseDirectory + path);
			else
				this.window.Icon = new Icon (path);
		}

		/// <summary>
		/// Creates a new Window
		/// </summary>
		/// <param name="width">internal window's width</param>
		/// <param name="height">internal window's height</param>
		/// <param name="title">window's title</param>
		/// <param name="format">Pixel Format</param>
		public Window (int width, int height, string title, PixelFormat format)
		{
			this.window = new GameWindow (width, height, OpenTK.Graphics.GraphicsMode.Default, title, GameWindowFlags.FixedWindow, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default);

			this.width = width;
			this.height = height;

			this.pixelFormat = format;

			switch (format) {
			case PixelFormat.RGB:
				this.bitmap = new byte[width * height * 3];
				break;
			case PixelFormat.RGBA:
				this.bitmap = new byte[width * height * 4];
				break;
			default:
				throw new Exception ("Unsupported PixelFormat");
			}
				
			watch = new Stopwatch ();

			this.vertexArrayId = GL.GenVertexArray ();
			GL.BindVertexArray (this.vertexArrayId);

			this.shaderProgramId = this.AddShader (@"
#version 330 core
layout(location = 0) in vec2 vertex;
layout(location = 1) in vec2 uv;
out vec2 uvout;
void main(){
        gl_Position = vec4(vertex.xy, 0.0, 1.0);
        uvout = uv;
}", @"
#version 330 core
in vec2 uvout;
out vec4 color;
uniform sampler2D tex;
void main(){
        color = texture(tex, uvout);
}
");

			// vertex array
			int vBufferId = GL.GenBuffer ();
			GL.BindBuffer (BufferTarget.ArrayBuffer, vBufferId);
			float[] v = new float[] { -1, 1, -1, -1, 1, 1, 1, -1};
			GL.BufferData<float> (BufferTarget.ArrayBuffer, (IntPtr)(v.Length * sizeof(float)), v, BufferUsageHint.StaticDraw);

			// uv array
			int uvBufferId = GL.GenBuffer ();
			GL.BindBuffer (BufferTarget.ArrayBuffer, uvBufferId);
			float[] uv = new float[] { 0, 0, 0, 1, 1, 0, 1, 1 };
			GL.BufferData<float> (BufferTarget.ArrayBuffer, (IntPtr)(uv.Length * sizeof(float)), uv, BufferUsageHint.StaticDraw);

			// texture
			GL.ActiveTexture (TextureUnit.Texture0);
			this.textureId = GL.GenTexture ();
			GL.BindTexture (TextureTarget.Texture2D, this.textureId);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

			// attrib arrays
			GL.UseProgram (this.shaderProgramId);

			GL.EnableVertexAttribArray (0);
			GL.BindBuffer (BufferTarget.ArrayBuffer, vBufferId);
			GL.VertexAttribPointer (0, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

			GL.EnableVertexAttribArray (1);
			GL.BindBuffer (BufferTarget.ArrayBuffer, uvBufferId);
			GL.VertexAttribPointer (1, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

			GL.Uniform1 (GL.GetUniformLocation (this.shaderProgramId, "tex"), 0);

			// setup viewport
			GL.Viewport (0, 0, width, height);
			// required for updating context !
			this.window.Context.Update (this.window.WindowInfo);
			GL.Clear (ClearBufferMask.ColorBufferBit);
			this.window.SwapBuffers ();

			this.window.Closed += new EventHandler<EventArgs> (this.Close);
			this.window.Visible = true;

		}

		private int AddShader (string vertexShader, string fragmentShader)
		{
			int vertexShaderId = GL.CreateShader (ShaderType.VertexShader);
			int fragmentShaderId = GL.CreateShader (ShaderType.FragmentShader);

			GL.ShaderSource (vertexShaderId, vertexShader);
			GL.CompileShader (vertexShaderId);

			GL.ShaderSource (fragmentShaderId, fragmentShader);
			GL.CompileShader (fragmentShaderId);

			int programId = GL.CreateProgram ();
			GL.AttachShader (programId, vertexShaderId);
			GL.AttachShader (programId, fragmentShaderId);

			GL.LinkProgram (programId);

			GL.DetachShader (programId, vertexShaderId);
			GL.DetachShader (programId, fragmentShaderId);

			GL.DeleteShader (vertexShaderId);
			GL.DeleteShader (fragmentShaderId);

			return programId;
		}

		/// <summary>
		/// Returns mouse X position relative to the form
		/// </summary>
		public int mouseX {
			get {
				return this.window.Mouse.X;
			}
		}

		/// <summary>
		/// Returns mouse Y position relative to the form
		/// </summary>
		public int mouseY {
			get {
				return this.window.Mouse.Y;
			}
		}

		/// <summary>
		/// Returns <c>true</c> if mouse left button is pressed, otherwise <c>false</c>
		/// </summary>
		public bool mouseLeft {
			get {
				return this._mouseState.IsButtonDown (MouseButton.Left);
			}
		}

		/// <summary>
		/// Returns <c>true</c> if mouse right button is pressed, otherwise <c>false</c>
		/// </summary>
		public bool mouseRight {
			get {
				return this._mouseState.IsButtonDown (MouseButton.Right);
			}
		}

		/// <summary>
		/// Returns <c>true</c> if mouse middle button is pressed, otherwise <c>false</c>
		/// </summary>
		public bool mouseMiddle {
			get {
				return this._mouseState.IsButtonDown (MouseButton.Middle);
			}
		}



		private void Close (object sender, EventArgs args)
		{
			this.opened = false;
		}


		/// <summary>
		/// Returns true when <c>key</c> is pressed
		/// </summary>
		/// <param name="key">key to check if is pressed</param>
		public bool GetKey (KeyCode key)
		{
			return this._keyboardState.IsKeyDown ((Key)key);
		}

		/// <summary>
		/// Draws the current <c>Window.bitmap</c> into the form
		/// </summary>
		public void Blit ()
		{
			if (!this.watch.IsRunning)
				this.watch.Start ();



			this._keyboardState = Keyboard.GetState ();
			this._mouseState = Mouse.GetState ();

			PixelInternalFormat _internalFormat = PixelInternalFormat.Rgba;
			OpenTK.Graphics.OpenGL.PixelFormat _format = OpenTK.Graphics.OpenGL.PixelFormat.Rgba;

			switch (this.pixelFormat) {
			case PixelFormat.RGB:
				_internalFormat = PixelInternalFormat.Rgb;
				_format = OpenTK.Graphics.OpenGL.PixelFormat.Rgb;
				break;
			case PixelFormat.RGBA:
				_internalFormat = PixelInternalFormat.Rgba;
				_format = OpenTK.Graphics.OpenGL.PixelFormat.Rgba;
				break;
			default:
				throw new Exception ("Unsupported PixelFormat");
			}

			GL.Clear (ClearBufferMask.ColorBufferBit);

			GL.TexImage2D (TextureTarget.Texture2D, 0, _internalFormat, this.width, this.height, 0, _format, PixelType.UnsignedByte, this.bitmap);

			GL.DrawArrays (PrimitiveType.TriangleStrip, 0, 4);



			// redraw
			this.window.SwapBuffers ();

			// get next events
			this.window.ProcessEvents ();

			this._deltaTime = (float)this.watch.Elapsed.TotalSeconds;


			this.watch.Reset ();
			this.watch.Start ();
		}
	}
}


