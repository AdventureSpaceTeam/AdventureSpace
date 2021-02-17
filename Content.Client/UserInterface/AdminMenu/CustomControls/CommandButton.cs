﻿#nullable enable
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface.AdminMenu.CustomControls
{
    public class CommandButton : Button
    {
        public string? Command { get; set; }

        public CommandButton() : base()
        {
            OnPressed += Execute;
        }

        protected virtual bool CanPress()
        {
            return string.IsNullOrEmpty(Command) ||
                   IoCManager.Resolve<IClientConGroupController>().CanCommand(Command);
        }

        protected override void EnteredTree()
        {
            if (!CanPress())
            {
                Visible = false;
            }
        }

        protected virtual void Execute(ButtonEventArgs obj)
        {
            // Default is to execute command
            if (!string.IsNullOrEmpty(Command))
                IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(Command);
        }
    }
}
