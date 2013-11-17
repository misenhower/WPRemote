using Komodex.DACP.Databases;
using Komodex.DACP.Groups;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class iTunesUContainer : DACPContainer
    {
        public iTunesUContainer(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 2097152, 2097154, 2097156, 2097158 }; }
        }

        protected override string ItemsMeta
        {
            get { return base.ItemsMeta + ",daap.songdatereleased"; }
        }

        #region Courses

        private List<iTunesUCourse> _courses;
        private Dictionary<int, iTunesUCourse> _coursesByID;

        public async Task<List<iTunesUCourse>> GetCoursesAsync()
        {
            if (_courses != null)
                return _courses;

            _courses = null;
            _coursesByID = null;

            _courses = await GetGroupsAsync(GroupsQuery, n => new iTunesUCourse(this, n)).ConfigureAwait(false);
            if (_courses != null)
                _coursesByID = _courses.ToDictionary(c => c.ID);

            return _courses;
        }

        public async Task<iTunesUCourse> GetCourseByIDAsync(int courseID)
        {
            if (_coursesByID == null)
                await GetCoursesAsync().ConfigureAwait(false);
            if (_coursesByID == null || !_coursesByID.ContainsKey(courseID))
                return null;

            return _coursesByID[courseID];
        }

        #endregion

        #region Unplayed Courses

        public Task<List<iTunesUCourse>> GetUnplayedCoursesAsync()
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songuserplaycount", 0), GroupsQuery);
            return GetGroupsAsync(query, n => new iTunesUCourse(this, n));
        }

        #endregion
    }
}
