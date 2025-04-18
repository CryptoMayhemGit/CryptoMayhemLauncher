﻿using Mayhem.Dal.Dto.Dtos;

namespace Mayhem.Launcher.Models
{
    public class FileSettings
    {
        public string GamePath { get; set; }
        public string Wallet { get; set; }
        public string CurrentCulture { get; set; }
        public BuildVersion GameVersion { get; set; }
        public string InvestorTicket { get; set; }
    }
}
