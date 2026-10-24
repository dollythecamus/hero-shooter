namespace HeroShooter.Systems
{
    /// <summary>
    /// What a character-select card talks to. <see cref="MatchBootstrap"/> implements it when the select screen
    /// runs inside a match scene, <see cref="MainMenuUI"/> when it runs in the boot scene, so the cards and
    /// <see cref="CharacterSelectUI"/> stay unaware of which flow they are part of.
    /// </summary>
    public interface ICharacterPicker
    {
        void PickCharacter(int index);
    }
}
