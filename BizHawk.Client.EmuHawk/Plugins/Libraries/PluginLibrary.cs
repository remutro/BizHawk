﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk

{
	public class PluginLibrary
	{
//		public LuaDocumentation Docs { get; }
		public PluginLibrary(IEmulatorServiceProvider serviceProvider)
		{
			// Docs.Clear();

			// Register lua libraries
			var libs = Assembly
				.Load("BizHawk.Client.Common")
				.GetTypes()
				.Where(t => typeof(PluginLibraryBase).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.Where(t => ServiceInjector.IsAvailable(serviceProvider, t))
				.ToList();

			libs.AddRange(
				Assembly
				.GetAssembly(typeof(PluginLibrary))
				.GetTypes()
				.Where(t => typeof(PluginLibraryBase).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.Where(t => ServiceInjector.IsAvailable(serviceProvider, t)));

			foreach (var lib in libs)
			{
				bool addLibrary = true;
				//var attributes = lib.GetCustomAttributes(typeof(LuaLibraryAttribute), false);
				//if (attributes.Any())
				//{
				//	addLibrary = VersionInfo.DeveloperBuild || (attributes.First() as LuaLibraryAttribute).Released;
				//}

				if (addLibrary)
				{
					var instance = (PluginLibraryBase)Activator.CreateInstance(lib);
					//instance.LuaRegister(lib, Docs);
					//instance.LogOutputCallback = ConsoleLuaLibrary.LogOutput;
					ServiceInjector.UpdateServices(serviceProvider, instance);
					Libraries.Add(lib, instance);
				}
			}
		}
		private readonly Dictionary<Type, PluginLibraryBase> Libraries = new Dictionary<Type, PluginLibraryBase>();
		public List<PluginBase> PluginList { get; } = new List<PluginBase>();

		public IEnumerable<PluginBase> RunningPlugins
		{
			get { return PluginList.Where(plug => plug.Enabled); }
		}

//		private FormsPluginLibrary FormsLibrary => (FormsLuaLibrary)Libraries[typeof(FormsLuaLibrary)];
		private EmulatorPluginLibrary EmulatorLuaLibrary => (EmulatorPluginLibrary)Libraries[typeof(EmulatorPluginLibrary)];
		public GUIPluginLibrary GuiLibrary => (GUIPluginLibrary)Libraries[typeof(GUIPluginLibrary)];

		public void Restart(IEmulatorServiceProvider newServiceProvider)
		{
			foreach (var lib in Libraries)
			{
				ServiceInjector.UpdateServices(newServiceProvider, lib.Value);
			}
			foreach (var plugin in PluginList)
			{
				plugin.Init(new PluginAPI(Libraries));
			}
		}

		public void StartPluginDrawing()
		{
			if (PluginList.Any() && !GuiLibrary.HasGUISurface)
			{
				GuiLibrary.DrawNew("emu");
			}
		}

		public void EndPluginDrawing()
		{
			if (PluginList.Any())
			{
				GuiLibrary.DrawFinish();
			}
		}

		public void CallSaveStateEvent(string name)
		{
			foreach (var plugin in RunningPlugins) plugin.SaveStateCallback(name);
		}

		public void CallLoadStateEvent(string name)
		{
			foreach (var plugin in RunningPlugins) plugin.LoadStateCallback(name);
		}

		public void CallFrameBeforeEvent()
		{
			StartPluginDrawing();
			foreach (var plugin in RunningPlugins) plugin.PreFrameCallback();
		}

		public void CallFrameAfterEvent()
		{
			foreach (var plugin in RunningPlugins) plugin.PostFrameCallback();
			EndPluginDrawing();
		}

		public void CallExitEvent()
		{
			foreach (var plugin in RunningPlugins) plugin.ExitCallback();
		}

		public void Close()
		{
			GuiLibrary.Dispose();
		}
		
		public void Load(PluginBase plugin)
		{
			plugin.Init(new PluginAPI(Libraries));
			PluginList.Add(plugin);
		}
	}
}
