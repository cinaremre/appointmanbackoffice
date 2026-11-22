using System;
using System.Collections.Generic;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Events.Notification;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Title { get; private set; }
        public string Message { get; private set; }
        public EnumNotificationType Type { get; private set; }
        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }
        public DateTime SentAt { get; private set; }
        public string? ActionUrl { get; private set; }
        public Dictionary<string, string> Metadata { get; private set; }

        // Navigation Property
        public User User { get; private set; }

        private Notification() { } // EF Core

        public Notification(Guid userId, string title, string message,
                    EnumNotificationType type, string? actionUrl = null)
     : this(Guid.NewGuid(), userId, title, message, type, actionUrl)
        {
        }
        public Notification(Guid id, Guid userId, string title, string message,
                   EnumNotificationType type, string? actionUrl = null)
    : base(id)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Kullanici ID bos olamaz.", nameof(userId));

            UserId = userId;
            Type = type;
            SentAt = DateTime.UtcNow;
            IsRead = false;
            Metadata = new Dictionary<string, string>();

            SetTitle(title);
            SetMessage(message);
            SetActionUrl(actionUrl);

            AddDomainEvent(new NotificationSentEvent(Id, UserId, Type, Title));
        }
        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Baslik bos olamaz.");

            if (title.Length > 200)
                throw new DomainException("Baslik 200 karakterden uzun olamaz.");

            Title = title;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new DomainException("Mesaj bos olamaz.");

            if (message.Length > 1000)
                throw new DomainException("Mesaj 1000 karakterden uzun olamaz.");

            Message = message;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetActionUrl(string? actionUrl)
        {
            if (actionUrl != null && actionUrl.Length > 500)
                throw new DomainException("Action URL 500 karakterden uzun olamaz.");

            ActionUrl = actionUrl;
            UpdatedAt = DateTime.UtcNow;
        }
        public void MarkAsRead()
        {
            if (IsRead)
                return;

            IsRead = true;
            ReadAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new NotificationReadEvent(Id, UserId, Type, Title));
        }

        public void AddMetadata(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key bos olamaz.", nameof(key));

            Metadata[key] = value;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Delete(string? deletedBy = null)
        {
            if (IsDeleted)
                return;

            SoftDelete();
            AddDomainEvent(new NotificationDeletedEvent(Id, UserId, Type, Title, deletedBy));
        }

        // Okunmamis mi kontrol et
        public bool IsUnread()
        {
            return !IsRead;
        }

        // Bildirim özeti
        public string GetSummary()
        {
            return $"{SentAt:dd.MM.yyyy HH:mm} - {Title} ({(IsRead ? "Okundu" : "Okunmadi")})";
        }
        public void SetSentAtForSeed(DateTime sentAt)
        {
            SentAt = sentAt;
        }
    }
}