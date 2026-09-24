# Campaign launch milestone

Implement the approved dark campaign library, creation form, and basic campaign page using MVVM.
Persist versioned JSON in Data/Campaigns/<id>/campaign.json beside the executable, without AppData fallback.

Acceptance: create and reopen campaigns across restarts and folder moves; sort by name, system, and last updated;
preserve selection; validate blank inputs; retain input on save failure; report partial/corrupt loads without destroying data.

Sequence: storage and tests; ViewModels and tests; WPF views; documentation; verification and UI smoke checks.
No editing/deletion, built-in rules, cloud storage, packaging, commits, or publishing in this milestone.
