using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Tarjim.Models;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<InstantTranslatorRequest> InstantTranslatorRequests { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectFile> ProjectFiles { get; set; }

    public virtual DbSet<ProjectOffer> ProjectOffers { get; set; }

    public virtual DbSet<ProjectRequirement> ProjectRequirements { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<SystemMessage> SystemMessages { get; set; }

    public virtual DbSet<TranslationCategory> TranslationCategories { get; set; }

    public virtual DbSet<TranslatorCv> TranslatorCvs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSetting> UserSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-CCI3ROF;Database=Tarjim;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Activity__9E2397E0718FAEB0");

            entity.ToTable("Activity_Log");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.ActivityType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("activity_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("entity_type");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Activity___user___0C85DE4D");
        });

        modelBuilder.Entity<InstantTranslatorRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__InstantT__33A8517A90F19DB9");

            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ServiceType).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.AssignedTranslator).WithMany(p => p.InstantTranslatorRequestAssignedTranslators)
                .HasForeignKey(d => d.AssignedTranslatorId)
                .HasConstraintName("FK_InstantTranslatorRequests_AssignedTranslator");

            entity.HasOne(d => d.Client).WithMany(p => p.InstantTranslatorRequestClients)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstantTranslatorRequests_Client");

            entity.HasOne(d => d.SourceLanguage).WithMany(p => p.InstantTranslatorRequestSourceLanguages)
                .HasForeignKey(d => d.SourceLanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstantTranslatorRequests_SourceLanguage");

            entity.HasOne(d => d.TargetLanguage).WithMany(p => p.InstantTranslatorRequestTargetLanguages)
                .HasForeignKey(d => d.TargetLanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InstantTranslatorRequests_TargetLanguage");
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.LanguageId).HasName("PK__Language__804CF6B320675B0A");

            entity.HasIndex(e => e.LanguageCode, "UQ__Language__A6D3AFDBDE38254F").IsUnique();

            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.LanguageCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("language_code");
            entity.Property(e => e.LanguageName)
                .HasMaxLength(100)
                .HasColumnName("language_name");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Messages__0BBF6EE66113A23C");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");

            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Receiver");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Sender");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842F5335F67B");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.LinkUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("link_url");
            entity.Property(e => e.Message)
                .HasMaxLength(500)
                .HasColumnName("message");
            entity.Property(e => e.RelatedId).HasColumnName("related_id");
            entity.Property(e => e.RelatedType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("related_type");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__user___778AC167");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__ED1FC9EA3E622956");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payment_method");
            entity.Property(e => e.PlatformFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("platform_fee");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("transaction_id");
            entity.Property(e => e.TranslatorId).HasColumnName("translator_id");

            entity.HasOne(d => d.Client).WithMany(p => p.PaymentClients)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__client__7C4F7684");

            entity.HasOne(d => d.Project).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__projec__7B5B524B");

            entity.HasOne(d => d.Translator).WithMany(p => p.PaymentTranslators)
                .HasForeignKey(d => d.TranslatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__transl__7D439ABD");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__BC799E1F5408A0DA");

            entity.ToTable(tb => tb.HasTrigger("trg_NewProjectNotification"));

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.AssignedTranslatorId).HasColumnName("assigned_translator_id");
            entity.Property(e => e.Budget)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("budget");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Deadline)
                .HasColumnType("datetime")
                .HasColumnName("deadline");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("file_url");
            entity.Property(e => e.PageCount).HasColumnName("page_count");
            entity.Property(e => e.SourceLanguageId).HasColumnName("source_language_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Open")
                .HasColumnName("status");
            entity.Property(e => e.TargetLanguageId).HasColumnName("target_language_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.AssignedTranslator).WithMany(p => p.ProjectAssignedTranslators)
                .HasForeignKey(d => d.AssignedTranslatorId)
                .HasConstraintName("FK__Projects__assign__5070F446");

            entity.HasOne(d => d.Category).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Projects__catego__534D60F1");

            entity.HasOne(d => d.Client).WithMany(p => p.ProjectClients)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Projects__client__4F7CD00D");

            entity.HasOne(d => d.SourceLanguage).WithMany(p => p.ProjectSourceLanguages)
                .HasForeignKey(d => d.SourceLanguageId)
                .HasConstraintName("FK__Projects__source__5165187F");

            entity.HasOne(d => d.TargetLanguage).WithMany(p => p.ProjectTargetLanguages)
                .HasForeignKey(d => d.TargetLanguageId)
                .HasConstraintName("FK__Projects__target__52593CB8");
        });

        modelBuilder.Entity<ProjectFile>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK__Project___07D884C60C00A559");

            entity.ToTable("Project_Files");

            entity.Property(e => e.FileId).HasColumnName("file_id");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("file_path");
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.FileType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("file_type");
            entity.Property(e => e.IsOriginal)
                .HasDefaultValue(true)
                .HasColumnName("is_original");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectFiles)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Project_F__proje__5812160E");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.ProjectFiles)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Project_F__uploa__59063A47");
        });

        modelBuilder.Entity<ProjectOffer>(entity =>
        {
            entity.HasKey(e => e.OfferId).HasName("PK__Project___03D37AC2EFFDDB88");

            entity.ToTable("Project_Offers", tb => tb.HasTrigger("trg_NewOfferNotification"));

            entity.Property(e => e.OfferId).HasColumnName("offer_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveryDate)
                .HasColumnType("datetime")
                .HasColumnName("delivery_date");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.OfferStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("offer_status");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProposedFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("proposed_fee");
            entity.Property(e => e.TranslatorId).HasColumnName("translator_id");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectOffers)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Project_O__proje__619B8048");

            entity.HasOne(d => d.Translator).WithMany(p => p.ProjectOffers)
                .HasForeignKey(d => d.TranslatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Project_O__trans__628FA481");
        });

        modelBuilder.Entity<ProjectRequirement>(entity =>
        {
            entity.HasKey(e => e.RequirementId).HasName("PK__Project___2A73C1AD0F23395A");

            entity.ToTable("Project_Requirements");

            entity.Property(e => e.RequirementId).HasColumnName("requirement_id");
            entity.Property(e => e.IsRequired)
                .HasDefaultValue(false)
                .HasColumnName("is_required");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RequirementLabel)
                .HasMaxLength(200)
                .HasColumnName("requirement_label");
            entity.Property(e => e.RequirementType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("requirement_type");
            entity.Property(e => e.RequirementValue)
                .HasMaxLength(500)
                .HasColumnName("requirement_value");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectRequirements)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Project_R__proje__5CD6CB2B");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__60883D903A280D57");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateRating"));

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.TranslatorId).HasColumnName("translator_id");

            entity.HasOne(d => d.Client).WithMany(p => p.ReviewClients)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__client___693CA210");

            entity.HasOne(d => d.Project).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__project__6754599E");

            entity.HasOne(d => d.Translator).WithMany(p => p.ReviewTranslators)
                .HasForeignKey(d => d.TranslatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__transla__68487DD7");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__760965CC378D622F");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__783254B1EFBA4CE5").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId).HasName("PK__Skills__FBBA8379438FFB8D");

            entity.HasIndex(e => e.SkillName, "UQ__Skills__73C038AD60A0A467").IsUnique();

            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .HasColumnName("skill_name");
        });

        modelBuilder.Entity<SystemMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__System_M__0BBF6EE6D73040DC");

            entity.ToTable("System_Messages");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");

            entity.HasOne(d => d.Project).WithMany(p => p.SystemMessages)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__System_Me__proje__01142BA1");

            entity.HasOne(d => d.Sender).WithMany(p => p.SystemMessages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__System_Me__sende__02084FDA");
        });

        modelBuilder.Entity<TranslationCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Translat__D54EE9B464D26A39");

            entity.ToTable("Translation_Categories");

            entity.HasIndex(e => e.CategoryName, "UQ__Translat__5189E25553104C22").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
        });

        modelBuilder.Entity<TranslatorCv>(entity =>
        {
            entity.HasKey(e => e.CvId).HasName("PK__Translat__C36883E605478BE9");

            entity.ToTable("TranslatorCV");

            entity.Property(e => e.CvId).HasColumnName("cv_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("note");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.TranslatorId).HasColumnName("translator_id");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.Value)
                .HasMaxLength(500)
                .HasColumnName("value");

            entity.HasOne(d => d.Translator).WithMany(p => p.TranslatorCvs)
                .HasForeignKey(d => d.TranslatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Translato__trans__72C60C4A");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370FB2E63293");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164FF072836").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AccountType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Personal")
                .HasColumnName("account_type");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.Bio)
                .HasMaxLength(1000)
                .HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_verified");
            entity.Property(e => e.LastLogin)
                .HasColumnType("datetime")
                .HasColumnName("last_login");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Rating)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("rating");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__User_Role__role___440B1D61"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__User_Role__user___4316F928"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__User_Rol__6EDEA153B28E345C");
                        j.ToTable("User_Roles");
                        j.IndexerProperty<int>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<int>("RoleId").HasColumnName("role_id");
                    });

            entity.HasMany(d => d.Skills).WithMany(p => p.Translators)
                .UsingEntity<Dictionary<string, object>>(
                    "TranslatorSkill",
                    r => r.HasOne<Skill>().WithMany()
                        .HasForeignKey("SkillId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Translato__skill__6FE99F9F"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("TranslatorId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Translato__trans__6EF57B66"),
                    j =>
                    {
                        j.HasKey("TranslatorId", "SkillId").HasName("PK__Translat__4D31DDA19E6E698E");
                        j.ToTable("Translator_Skills");
                        j.IndexerProperty<int>("TranslatorId").HasColumnName("translator_id");
                        j.IndexerProperty<int>("SkillId").HasColumnName("skill_id");
                    });
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User_Set__B9BE370F6FC46EDF");

            entity.ToTable("User_Settings");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.LanguagePreference)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ar")
                .HasColumnName("language_preference");
            entity.Property(e => e.NotificationEmail)
                .HasDefaultValue(true)
                .HasColumnName("notification_email");
            entity.Property(e => e.NotificationSystem)
                .HasDefaultValue(true)
                .HasColumnName("notification_system");
            entity.Property(e => e.Theme)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("light")
                .HasColumnName("theme");

            entity.HasOne(d => d.User).WithOne(p => p.UserSetting)
                .HasForeignKey<UserSetting>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User_Sett__user___08B54D69");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
