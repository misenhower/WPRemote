using Komodex.DACP.Databases;
using Komodex.DACP.Groups;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class iTunesUContainer : DacpContainer
    {
        public iTunesUContainer(DacpDatabase database, DacpNodeDictionary nodes)
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
        private int _courseCacheRevision;

        public async Task<List<iTunesUCourse>> GetCoursesAsync()
        {
            if (_courses != null && _courseCacheRevision == Client.CurrentLibraryUpdateNumber)
                return _courses;

            _courses = null;
            _coursesByID = null;

            _courses = await GetGroupsAsync(GroupsQuery, n => new iTunesUCourse(this, n)).ConfigureAwait(false);
            if (_courses != null)
                _coursesByID = _courses.ToDictionary(c => c.ID);

            _courseCacheRevision = Client.CurrentLibraryUpdateNumber;

            return _courses;
        }

        public async Task<iTunesUCourse> GetCourseByIDAsync(int courseID)
        {
            await GetCoursesAsync().ConfigureAwait(false);
            if (_coursesByID == null || !_coursesByID.ContainsKey(courseID))
                return null;

            return _coursesByID[courseID];
        }

        #endregion

        #region Unplayed Courses

        public Task<List<iTunesUCourse>> GetUnplayedCoursesAsync()
        {
            var query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songuserplaycount", 0), GroupsQuery);
            return GetGroupsAsync(query, n => new iTunesUCourse(this, n));
        }

        #endregion

        #region Course Search

        public async Task<List<iTunesUCourse>> SearchCoursesAsync(string searchString, CancellationToken cancellationToken)
        {
            DacpQueryElement query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songalbum", searchString), DacpQueryPredicate.IsNotEmpty("daap.songalbum"), MediaKindQuery);
            DacpRequest request = GetGroupsRequest(query, false, "albums");
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                return DacpUtility.GetItemsFromNodes(response.Nodes, n => new iTunesUCourse(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion
    }
}
