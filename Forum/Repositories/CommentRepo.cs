﻿using Forum.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Forum.Helpers;

namespace Forum.Repositories
{
    public class CommentRepo:BaseRepo
    {

        public static List<Comment> GetComments(int post_id)
        {
            SqlConnection con = new SqlConnection(connectionString);
            SqlCommand cmd = con.CreateCommand();
            string query = "WITH n AS" +
                "(Select p.comment_id, p.parent_id , 1 as level, p.content,p.created_at,u.user_id, u.username " +
                "FROM [Comment] p INNER JOIN [User] u ON p.user_id = u.user_id " +
                "WHERE  post_id = @ID and parent_id is null " +
                "UNION ALL " +
                "SELECT m.comment_id, m.parent_id, level + 1, m.content,m.created_at, u.user_id, u.username "+
                "FROM [Comment] m INNER JOIN n  on m.parent_id = n.comment_id INNER JOIN [User] u ON m.user_id = u.user_id "+
                ")Select  * from n ORDER BY level, parent_id, created_at; ";
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("ID", post_id);
            Comment root = new Comment { CommentId = 0, Level = 0 };
            try
            {
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.HasRows)
                {
                    reader.Read();
                    int parentId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    Comment comment = new Comment
                    {
                        CommentId = reader.GetInt32(0),
                        ParentId = parentId,
                        Level = reader.GetInt32(2),
                        Content = reader.GetString(3),
                        CreatedAt = reader.GetDateTime(4),
                        UserId = reader.GetInt32(5),
                        Users = new User { UserId = reader.GetInt32(5), Username = reader.GetString(6), }
                    };
                    root.TryInsert(comment.Level, comment);
                }
                reader.Close();

            }catch(Exception ex)
            {

            }
            finally
            {
                cmd.Dispose();
                con.Close();
            }
            return root.Children;
        }
    }
}