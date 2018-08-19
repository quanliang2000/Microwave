﻿using System;
using System.Collections.Generic;

namespace Application.Seasons
{
    public class DomainValidationException : Exception
    {
        public IEnumerable<string> DomainErrors { get; }

        public DomainValidationException(IEnumerable<string> domainErrors) : base($"Validation failed because of: {string.Join("", "", domainErrors)}")
        {
            DomainErrors = domainErrors;
        }
    }
}