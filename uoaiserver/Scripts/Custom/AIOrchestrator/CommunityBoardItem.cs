using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;

namespace Server.Items
{
    /// <summary>
    /// A community board where players can post and browse requests for help, trading, grouping, and RP activities.
    /// Posts expire after 24 hours real-time.
    /// </summary>
    public class CommunityBoardItem : Item
    {
        private readonly List<CommunityPost> _posts = new List<CommunityPost>();
        private DateTime _lastCleanup = DateTime.UtcNow;

        [Constructable]
        public CommunityBoardItem() : base(0x1E5E)
        {
            Name = "Community Board";
            Hue = 0x3B2;
            Weight = 5.0;
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && from.InRange(GetWorldLocation(), 4))
            {
                CleanupExpired();
                from.SendGump(new CommunityBoardGump(pm, this));
            }
        }

        /// <summary>Add a post from a player.</summary>
        public void AddPost(PlayerMobile author, string title, string body)
        {
            _posts.Add(new CommunityPost
            {
                Author = author.Name,
                AuthorSerial = author.Serial,
                Title = title,
                Body = body,
                Created = DateTime.UtcNow
            });
        }

        /// <summary>Get all active (non-expired) posts.</summary>
        public List<CommunityPost> GetActivePosts()
        {
            CleanupExpired();
            return new List<CommunityPost>(_posts);
        }

        /// <summary>Remove a single post by index.</summary>
        public void RemovePost(int index)
        {
            if (index >= 0 && index < _posts.Count)
                _posts.RemoveAt(index);
        }

        private void CleanupExpired()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromHours(24);
            _posts.RemoveAll(p => p.Created < cutoff);
            _lastCleanup = DateTime.UtcNow;
        }

        // ── Serialization ────────────────────────────────────────────

        public CommunityBoardItem(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(_posts.Count);
            foreach (var p in _posts)
            {
                writer.Write(p.Author ?? "");
                writer.Write(p.AuthorSerial.Value);
                writer.Write(p.Title ?? "");
                writer.Write(p.Body ?? "");
                writer.Write(p.Created.Ticks);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int v = reader.ReadInt();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                _posts.Add(new CommunityPost
                {
                    Author = reader.ReadString(),
                    AuthorSerial = reader.ReadInt(),
                    Title = reader.ReadString(),
                    Body = reader.ReadString(),
                    Created = new DateTime(reader.ReadLong(), DateTimeKind.Utc)
                });
            }
        }
    }

    /// <summary>A single post on the community board.</summary>
    public class CommunityPost
    {
        public string Author { get; set; }
        public Serial AuthorSerial { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime Created { get; set; }

        public bool IsExpired
        {
            get { return DateTime.UtcNow - Created > TimeSpan.FromHours(24); }
        }
    }

    // ── Gump ─────────────────────────────────────────────────────────

    public class CommunityBoardGump : Gump
    {
        private readonly CommunityBoardItem _board;

        public CommunityBoardGump(PlayerMobile pm, CommunityBoardItem board) : base(50, 50)
        {
            _board = board;
            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 500, 500, 9250);
            AddHtml(0, 10, 500, 30, "<center><big><b>Community Board</b></big></center>", false, false);

            int y = 50;
            var posts = _board.GetActivePosts();

            if (posts.Count == 0)
            {
                AddHtml(40, y, 420, 40, "<i>No posts yet. Be the first!</i>", false, false);
                y += 50;
            }
            else
            {
                for (int i = 0; i < posts.Count && y < 400; i++)
                {
                    var p = posts[i];
                    string html = string.Format("<b>[{0}]</b> <u>{1}</u> by <i>{2}</i><br>{3}", i + 1, p.Title, p.Author, p.Body);
                    AddHtml(40, y, 380, 60, html, false, true);
                    AddButton(430, y + 5, 4017, 4018, i + 100, GumpButtonType.Reply, 0);
                    y += 70;
                }
            }

            AddButton(40, y + 10, 4020, 4021, 1, GumpButtonType.Reply, 0);
            AddLabel(70, y + 10, 0x44, "New Post");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!(sender.Mobile is PlayerMobile pm)) return;

            int bid = info.ButtonID;

            if (bid == 1)
            {
                // New Post
                pm.SendMessage(0x44, "Enter a title for your post:");
                pm.Prompt = new PostTitlePrompt(_board);
                return;
            }

            if (bid >= 100)
            {
                int idx = bid - 100;
                // View/delete post
                var posts = _board.GetActivePosts();
                if (idx >= 0 && idx < posts.Count)
                {
                    var p = posts[idx];
                    pm.SendGump(new PostDetailGump(p, idx, _board));
                }
            }
        }
    }

    public class PostTitlePrompt : Server.Prompts.Prompt
    {
        private readonly CommunityBoardItem _board;
        public PostTitlePrompt(CommunityBoardItem board) { _board = board; }

        public override void OnResponse(Mobile from, string text)
        {
            if (from is PlayerMobile pm && !string.IsNullOrWhiteSpace(text))
            {
                pm.SendMessage(0x44, "Now enter the body of your post:");
                pm.Prompt = new PostBodyPrompt(_board, text.Trim());
            }
        }
    }

    public class PostBodyPrompt : Server.Prompts.Prompt
    {
        private readonly CommunityBoardItem _board;
        private readonly string _title;
        public PostBodyPrompt(CommunityBoardItem board, string title) { _board = board; _title = title; }

        public override void OnResponse(Mobile from, string text)
        {
            if (from is PlayerMobile pm && !string.IsNullOrWhiteSpace(text))
            {
                _board.AddPost(pm, _title, text.Trim());
                pm.SendMessage(0x44, "Your post has been added to the community board!");
            }
        }
    }

    public class PostDetailGump : Gump
    {
        private readonly CommunityPost _post;
        private readonly int _index;
        private readonly CommunityBoardItem _board;

        public PostDetailGump(CommunityPost post, int index, CommunityBoardItem board) : base(100, 100)
        {
            _post = post;
            _index = index;
            _board = board;
            Closable = true;
            Dragable = true;

            AddPage(0);
            AddBackground(0, 0, 400, 250, 9250);
            AddHtml(20, 20, 360, 20, string.Format("<big><b>{0}</b></big>", post.Title), false, false);
            AddHtml(20, 45, 360, 20, string.Format("Posted by: {0}", post.Author), false, false);
            AddHtml(20, 70, 360, 100, post.Body, false, true);

            AddButton(40, 200, 4017, 4018, 1, GumpButtonType.Reply, 0);
            AddLabel(70, 200, 0x26, "Delete");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (sender.Mobile is PlayerMobile pm && info.ButtonID == 1)
            {
                _board.RemovePost(_index);
                pm.SendMessage(0x44, "Post removed.");
            }
        }
    }
}