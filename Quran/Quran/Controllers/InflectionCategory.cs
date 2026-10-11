using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quran.Controllers
{
    public enum InflectionCategory
    {
        FormI,
        FormII,
        FormIII,
        FormIV,
        FormV,
        FormVI,
        FormVII,
        FormVIII,
        FormIX,
        FormX,
        Nominal,        // nouns, adjectives
        Participle,     // active/passive participles
        PrefixMu,       // mu- prefix
        PrefixTa,       // ta- prefix
        PrefixIst,      // ist- prefix
        PluralSound,    // sound plural
        PluralBroken,   // broken plural
        CaseNominative,
        CaseAccusative,
        CaseGenitive
    }
}
