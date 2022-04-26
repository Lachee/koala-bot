using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KoalaBot.Interactivity
{
    internal class PaginationRequest : IPaginationRequest
    {
        private TaskCompletionSource<bool> _tcs;
        private CancellationTokenSource _ct;
        private TimeSpan _timeout;
        private List<Page> _pages;
        private PaginationBehaviour _behaviour;
        private PaginationDeletion _deletion;
        private DiscordMessage _message;
        private PaginationEmojis _emojis;
        private DiscordUser _user;
        private int index = 0;

        public int PageCount
        {
            get
            {
                if (_pages == null)
                    throw new ArgumentNullException(nameof(_pages), "Could not get PageCount for Pagination.");

                return _pages.Count;
            }
        }

        /// <summary>
        /// Creates a new Pagination request
        /// </summary>
        /// <param name="message">Message to paginate</param>
        /// <param name="user">User to allow control for</param>
        /// <param name="behaviour">Behaviour during pagination</param>
        /// <param name="deletion">Behavior on pagination end</param>
        /// <param name="emojis">Emojis for this pagination object</param>
        /// <param name="timeout">Timeout time</param>
        /// <param name="pages">Pagination pages</param>
        internal PaginationRequest(DiscordMessage message, DiscordUser user, PaginationBehaviour behaviour, PaginationDeletion deletion,
            PaginationEmojis emojis, TimeSpan timeout, params Page[] pages)
        {
            this._tcs = new TaskCompletionSource<bool>();
            this._ct = new CancellationTokenSource(timeout);
            this._ct.Token.Register(() => _tcs.TrySetResult(true));
            this._timeout = timeout;

            this._message = message;
            this._user = user;

            this._deletion = deletion;
            this._behaviour = behaviour;
            this._emojis = emojis;

            this._pages = new List<Page>();
            foreach (var p in pages)
            {
                this._pages.Add(p);
            }
        }

        public async Task<Page> GetPageAsync()
        {
            await Task.Yield();

            return _pages[index];
        }

        public async Task SkipLeftAsync()
        {
            await Task.Yield();

            index = 0;
        }

        public async Task SkipRightAsync()
        {
            await Task.Yield();

            index = _pages.Count - 1;
        }

        public async Task NextPageAsync()
        {
            await Task.Yield();

            switch (_behaviour)
            {
                case PaginationBehaviour.Default:
                case PaginationBehaviour.Ignore:
                    if (index == _pages.Count - 1)
                        break;
                    else
                        index++;

                    break;

                case PaginationBehaviour.WrapAround:
                    if (index == _pages.Count - 1)
                        index = 0;
                    else
                        index++;

                    break;
            }
        }

        public async Task PreviousPageAsync()
        {
            await Task.Yield();

            switch (_behaviour)
            {
                case PaginationBehaviour.Default:
                case PaginationBehaviour.Ignore:
                    if (index == 0)
                        break;
                    else
                        index--;

                    break;

                case PaginationBehaviour.WrapAround:
                    if (index == 0)
                        index = _pages.Count - 1;
                    else
                        index--;

                    break;
            }
        }

        public async Task<PaginationEmojis> GetEmojisAsync()
        {
            await Task.Yield();

            return this._emojis;
        }

        public async Task<DiscordMessage> GetMessageAsync()
        {
            await Task.Yield();

            return this._message;
        }

        public async Task<DiscordUser> GetUserAsync()
        {
            await Task.Yield();

            return this._user;
        }

        public async Task DoCleanupAsync()
        {
            switch (_deletion)
            {
                case PaginationDeletion.Default:
                case PaginationDeletion.DeleteEmojis:
                    await _message.DeleteAllReactionsAsync();
                    break;

                case PaginationDeletion.DeleteMessage:
                    await _message.DeleteAsync();
                    break;

                case PaginationDeletion.KeepEmojis:
                    break;
            }
        }

        public async Task<TaskCompletionSource<bool>> GetTaskCompletionSourceAsync()
        {
            await Task.Yield();

            return this._tcs;
        }

        ~PaginationRequest()
        {
            this.Dispose();
        }

        /// <summary>
        /// Disposes this PaginationRequest.
        /// </summary>
        public void Dispose()
        {
            this._ct.Dispose();
            this._tcs = null;
        }

        public Task<IEnumerable<DiscordButtonComponent>> GetButtonsAsync()
        {
#warning "This code is not complete and still needs implemented. Left placeholder code for stability."
            var x = (IEnumerable<DiscordButtonComponent>) new List<DiscordButtonComponent>();
            return Task.FromResult(x);
        }
    }
}