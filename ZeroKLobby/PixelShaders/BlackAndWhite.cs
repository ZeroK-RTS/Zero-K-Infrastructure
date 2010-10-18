using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ZeroKLobby.PixelShaders
{
	public class BlackAndWhite: ShaderEffect
	{
		public static readonly DependencyProperty FactorProperty = DependencyProperty.Register("Factor",
		                                                                                       typeof(float),
		                                                                                       typeof(BlackAndWhite),
		                                                                                       new UIPropertyMetadata(1f, PixelShaderConstantCallback(0)));
		public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(BlackAndWhite), 0);
		static readonly PixelShader loadedShader;

		public float Factor { get { return (float)GetValue(FactorProperty); } set { SetValue(FactorProperty, value); } }
		public Brush Input { get { return (Brush)GetValue(InputProperty); } set { SetValue(InputProperty, value); } }

		static BlackAndWhite()
		{
			var isDesigner = Process.GetCurrentProcess().ProcessName == "devenv";
			if (isDesigner) return;
			loadedShader = new PixelShader { UriSource = new Uri("PixelShaders/BlackAndWhite.ps", UriKind.Relative) };
		}
		

		public BlackAndWhite()
		{
			PixelShader = loadedShader;

			UpdateShaderValue(InputProperty);
			UpdateShaderValue(FactorProperty);
		}
	}
}