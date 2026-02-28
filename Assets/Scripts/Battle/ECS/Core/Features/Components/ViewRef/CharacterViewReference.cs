namespace Battle.ECS.Component
{
    public struct CharacterViewReference
    {
        public ICharacterView View;
        
        public CharacterViewReference(ICharacterView view)
        {
            View = view;
        }
    }
}