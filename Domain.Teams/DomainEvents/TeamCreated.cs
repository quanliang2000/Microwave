﻿using System;
using Domain.Framework;

namespace Domain.Team.DomainEvents
{
    public class TeamCreated : IDomainEvent
    {
        public Guid EntityId { get; }

        public Guid RaceId { get; }
        public string TeamName { get; }
        public string TrainerName { get; }

        public TeamCreated(Guid teamId, Guid raceId, string teamName, string trainerName)
        {
            EntityId = teamId;
            RaceId = raceId;
            TeamName = teamName;
            TrainerName = trainerName;
        }
    }
}