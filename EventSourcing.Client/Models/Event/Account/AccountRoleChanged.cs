﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kwetterprise.EventSourcing.Client.Models.Event.Account
{
    using Kwetterprise.EventSourcing.Client.Models.DataTransfer;

    public class AccountRoleChanged : EventBase
    {
        public AccountRoleChanged()
        {
            
        }

        public AccountRoleChanged(Guid target, Guid actor, AccountRole newRole)
            : base(EventType.AccountRoleChanged)
        {
            this.Target = target;
            this.Actor = actor;
            this.NewRole = newRole;
        }

        public Guid Target { get; set; }

        public Guid Actor { get;set; }

        public AccountRole NewRole { get; set; }
    }
}
