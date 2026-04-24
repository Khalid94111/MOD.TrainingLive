using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <summary>
    /// v4.6.0 — Phase 4A Patch 4 (Full Financial Entry at Creation).
    ///
    /// <para>
    /// This migration is intentionally empty. Patch 4 restructures the casual-course workflow
    /// (UTM now enters the full financial breakdown at creation) but does not change the database
    /// schema. The patch spec called for dropping <c>TrnCasualCourses.UTMPreliminaryEstimate</c>
    /// — that column never existed in this codebase, so there is nothing to drop.
    /// </para>
    /// <para>
    /// Kept as a named migration so the version bump v4.5.3 → v4.6.0 is discoverable in the
    /// migration history and so downstream environments advance past this checkpoint cleanly.
    /// </para>
    /// </summary>
    public partial class v4_6_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op — schema unchanged in v4.6.0.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op — schema unchanged in v4.6.0.
        }
    }
}
